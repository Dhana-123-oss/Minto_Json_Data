using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Minto.Api.Data;
using Minto.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Minto.Api.Identity;

/// <summary>Authentication and identity routes.</summary>
public static class IdentityEndpoints
{
    /// <summary>Maps login and current-user routes.</summary>
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/identity").WithTags("Identity");
        group.MapPost("/login", LoginAsync).WithName("Login").AllowAnonymous().Produces<LoginResponse>().ProducesProblem(401);
        group.MapPost("/otp/request", RequestOtpAsync).WithName("RequestOtp").AllowAnonymous();
        group.MapPost("/otp/verify", VerifyOtpAsync).WithName("VerifyOtp").AllowAnonymous();
        group.MapGet("/me", (ClaimsPrincipal user) => Results.Ok(new MeResponse(user.FindFirstValue(ClaimTypes.NameIdentifier)!, user.FindFirstValue(ClaimTypes.Email)!, user.FindFirstValue(ClaimTypes.Role)!))).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> LoginAsync(LoginRequest request, IConfiguration configuration, MintoDbContext db, HttpContext http)
    {
        var user = DevUsers.All.FirstOrDefault(candidate => candidate.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase) && candidate.Password == request.Password);
        if (user is null) return Results.Unauthorized();
        db.LoginHistory.Add(new Domain.LoginHistory { UserId = user.Id, IpAddress = http.Connection.RemoteIpAddress?.ToString() });
        await db.SaveChangesAsync();
        var key = configuration["Jwt:SigningKey"] ?? "minto-development-signing-key-change-in-production-32chars";
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.Id), new Claim(ClaimTypes.Email, user.Email), new Claim(ClaimTypes.Role, user.Role), new Claim("name", user.Name) };
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken("minto", "minto-api", claims, expires: DateTime.UtcNow.AddHours(8), signingCredentials: credentials);
        return Results.Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), user.Id, user.Name, user.Email, user.Role));
    }

    private static async Task<IResult> RequestOtpAsync(OtpRequest request, MintoDbContext db, IWebHostEnvironment environment, CancellationToken ct)
    {
        var phone = new string(request.Phone.Where(char.IsDigit).ToArray());
        var user = DevUsers.All.FirstOrDefault(x => x.Phone == phone);
        if (user is null) return Results.NotFound(new { error = "No Minto account is registered for this mobile number." });
        var challenge = new OtpChallenge { Phone = phone, UserId = user.Id, Code = environment.IsDevelopment() ? "123456" : Random.Shared.Next(100000, 999999).ToString(), ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5) };
        db.OtpChallenges.Add(challenge); await db.SaveChangesAsync(ct);
        return Results.Ok(new { challengeId = challenge.Id, expiresAt = challenge.ExpiresAt, demoCode = environment.IsDevelopment() ? challenge.Code : null });
    }

    private static async Task<IResult> VerifyOtpAsync(OtpVerifyRequest request, IConfiguration configuration, MintoDbContext db, CancellationToken ct)
    {
        var challenge = await db.OtpChallenges.SingleOrDefaultAsync(x => x.Id == request.ChallengeId && !x.Used && x.ExpiresAt > DateTimeOffset.UtcNow, ct);
        if (challenge is null || challenge.Code != request.Code) return Results.Unauthorized();
        challenge.Used = true; await db.SaveChangesAsync(ct);
        var user = DevUsers.All.Single(x => x.Id == challenge.UserId); var token = CreateToken(user, configuration);
        return Results.Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), user.Id, user.Name, user.Email, user.Role));
    }

    private static JwtSecurityToken CreateToken(DevUser user, IConfiguration configuration)
    {
        var key = configuration["Jwt:SigningKey"] ?? "minto-development-signing-key-change-in-production-32chars";
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.Id), new Claim(ClaimTypes.Email, user.Email), new Claim(ClaimTypes.Role, user.Role), new Claim("name", user.Name) };
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        return new JwtSecurityToken("minto", "minto-api", claims, expires: DateTime.UtcNow.AddHours(8), signingCredentials: credentials);
    }
}

/// <summary>Login request.</summary>
public sealed record LoginRequest(string Email, string Password);
public sealed record OtpRequest(string Phone);
public sealed record OtpVerifyRequest(Guid ChallengeId, string Code);
/// <summary>Login response.</summary>
public sealed record LoginResponse(string AccessToken, string UserId, string Name, string Email, string Role);
/// <summary>Current-user response.</summary>
public sealed record MeResponse(string UserId, string Email, string Role);

internal sealed record DevUser(string Id, string Name, string Email, string Password, string Role, string Phone);
internal static class DevUsers
{
    public static readonly DevUser[] All =
    [
        new("customer-1", "Minto Customer", "customer@minto.local", "Minto@123", "Customer", "9876543212"),
        new("picker-1", "Minto Picker", "picker@minto.local", "Minto@123", "Picker", "9876543211"),
        new("rider-1", "Minto Rider", "rider@minto.local", "Minto@123", "Rider", "9876543210"),
        new("manager-1", "Minto Manager", "manager@minto.local", "Minto@123", "Manager", "9876543213"),
        new("admin-1", "Minto Admin", "admin@minto.local", "Minto@123", "Admin", "9876543214")
    ];
}
