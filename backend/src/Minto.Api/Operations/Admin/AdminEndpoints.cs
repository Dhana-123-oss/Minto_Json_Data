using Microsoft.EntityFrameworkCore;

namespace Minto.Api.Operations.Admin;

/// <summary>Admin operational routes.</summary>
public static class AdminEndpoints
{
    /// <summary>Maps admin summary operations.</summary>
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/operations/admin").WithTags("Admin").RequireAuthorization("Admin"); group.MapGetSummary(); group.MapGet("/orders", async (Minto.Api.Data.MintoDbContext db, CancellationToken ct) => Results.Ok(await db.Orders.Include(x => x.Lines).OrderByDescending(x => x.CreatedAt).ToListAsync(ct))); group.MapTransitionOperation(); return app;
    }
}
