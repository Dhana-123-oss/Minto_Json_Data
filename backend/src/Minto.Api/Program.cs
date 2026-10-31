using MediatR;
using Microsoft.EntityFrameworkCore;
using Minto.Api.Data;
using Minto.Api.Features;
using Minto.Api.Commerce;
using Minto.Api.Infrastructure;
using Minto.Api.Operations;
using System.Text.Json.Serialization;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Minto.Api.Identity;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<MintoDbContext>(o =>
{
    if (builder.Environment.IsDevelopment()) o.UseInMemoryDatabase("minto-dev");
    else o.UseNpgsql(builder.Configuration.GetConnectionString("Minto") ?? "Host=localhost;Port=5432;Database=minto;Username=minto;Password=minto", npgsql => npgsql.EnableRetryOnFailure());
});
builder.Services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSignalR();
var jwtKey = builder.Configuration["Jwt:SigningKey"] ?? "minto-development-signing-key-change-in-production-32chars";
var keycloakAuthority = builder.Configuration["Keycloak:Authority"];
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    if (!string.IsNullOrWhiteSpace(keycloakAuthority)) { options.Authority = keycloakAuthority; options.Audience = builder.Configuration["Keycloak:Audience"] ?? "minto-api"; options.RequireHttpsMetadata = false; }
    else { options.TokenValidationParameters = new TokenValidationParameters { ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)), ValidateIssuer = true, ValidIssuer = "minto", ValidateAudience = true, ValidAudience = "minto-api", ValidateLifetime = true }; }
});
builder.Services.AddAuthorization(options => { options.AddPolicy("Customer", policy => policy.RequireRole("Customer")); options.AddPolicy("Picker", policy => policy.RequireRole("Picker", "Admin")); options.AddPolicy("Rider", policy => policy.RequireRole("Rider", "Admin")); options.AddPolicy("Admin", policy => policy.RequireRole("Admin")); });
builder.Services.AddHostedService<OutboxPublisher>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddHealthChecks().AddCheck("api", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());
var app = builder.Build();
app.UseCors();
app.UseAuthentication(); app.UseAuthorization();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.MapCommerceEndpoints();
app.MapOperationsEndpoints();
app.MapIdentityEndpoints();
app.MapHub<TrackingHub>("/hubs/tracking");
app.MapFeatureEndpoints();
app.MapHealthChecks("/health");
using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<MintoDbContext>(); await db.Database.EnsureCreatedAsync(); await Seed.SeedAsync(db); }
app.Run();

public partial class Program { }
