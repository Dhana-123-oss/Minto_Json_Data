using Microsoft.EntityFrameworkCore;
using Minto.Api.Commerce.Orders;
using Minto.Api.Data;
using Minto.Api.Domain;

namespace Minto.Api.Operations.Rider;

/// <summary><c>GET /api/v1/operations/rider/queue</c> — lists delivery-ready orders.</summary>
public static class ListRiderQueue
{
    /// <summary>Maps the rider queue route.</summary>
    public static IEndpointRouteBuilder MapListRiderQueue(this IEndpointRouteBuilder group)
    {
        group.MapGet("/queue", HandleAsync).WithName("ListRiderQueue").Produces<OrderResponse[]>(); return group;
    }

    private static async Task<IResult> HandleAsync(MintoDbContext db, CancellationToken ct) => Results.Ok((await db.Orders.Include(order => order.Lines)
        .Where(order => order.Status == OrderStatus.ReadyForPickup || order.Status == OrderStatus.Assigned || order.Status == OrderStatus.Accepted)
        .OrderBy(order => order.CreatedAt).ToArrayAsync(ct)).Select(OrderResponse.From).ToArray());
}
