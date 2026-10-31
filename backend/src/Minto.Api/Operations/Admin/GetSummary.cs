using Microsoft.EntityFrameworkCore;
using Minto.Api.Data;

namespace Minto.Api.Operations.Admin;

/// <summary><c>GET /api/v1/operations/admin/summary</c> — returns workflow counters.</summary>
public static class GetSummary
{
    /// <summary>Maps the admin summary route.</summary>
    public static IEndpointRouteBuilder MapGetSummary(this IEndpointRouteBuilder group)
    {
        group.MapGet("/summary", HandleAsync).WithName("GetOperationsSummary").Produces<OperationsSummary>(); return group;
    }

    private static async Task<IResult> HandleAsync(MintoDbContext db, CancellationToken ct) => Results.Ok(new OperationsSummary(
        await db.Products.CountAsync(product => product.IsActive, ct), await db.Inventory.SumAsync(item => item.Available, ct),
        await db.Orders.CountAsync(ct), await db.Orders.CountAsync(order => order.Status == Minto.Api.Domain.OrderStatus.Delivered, ct)));
}

/// <summary>Operational dashboard counters.</summary>
public sealed record OperationsSummary(int ActiveProducts, int AvailableUnits, int TotalOrders, int DeliveredOrders);
