using Microsoft.EntityFrameworkCore;
using Minto.Api.Data;
using Minto.Api.Commerce.Orders;
using Minto.Api.Domain;

namespace Minto.Api.Operations.Picker;

/// <summary><c>GET /api/v1/operations/picker/queue</c> — lists orders waiting for picking.</summary>
public static class ListPickerQueue
{
    /// <summary>Maps the picker queue route.</summary>
    public static IEndpointRouteBuilder MapListPickerQueue(this IEndpointRouteBuilder group)
    {
        group.MapGet("/queue", HandleAsync).WithName("ListPickerQueue").Produces<OrderResponse[]>(); return group;
    }

    private static async Task<IResult> HandleAsync(MintoDbContext db, CancellationToken ct) => Results.Ok((await db.Orders.Include(order => order.Lines)
        .Where(order => order.Status == OrderStatus.Placed || order.Status == OrderStatus.Picking).OrderBy(order => order.CreatedAt).ToArrayAsync(ct))
        .Select(OrderResponse.From).ToArray());
}
