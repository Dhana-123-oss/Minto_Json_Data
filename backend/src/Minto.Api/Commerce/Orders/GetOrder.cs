using Microsoft.EntityFrameworkCore;
using Minto.Api.Data;

namespace Minto.Api.Commerce.Orders;

/// <summary><c>GET /api/v1/commerce/orders/{id}</c> — gets one order.</summary>
public static class GetOrder
{
    /// <summary>Maps the order-detail route.</summary>
    public static IEndpointRouteBuilder MapGetOrder(this IEndpointRouteBuilder group)
    {
        group.MapGet("/{id:guid}", HandleAsync).WithName("GetOrder").Produces<OrderResponse>().ProducesProblem(404);
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid id, MintoDbContext db, CancellationToken ct) =>
        await db.Orders.Include(order => order.Lines).SingleOrDefaultAsync(order => order.Id == id, ct) is { } order
            ? Results.Ok(OrderResponse.From(order)) : Results.NotFound();
}
