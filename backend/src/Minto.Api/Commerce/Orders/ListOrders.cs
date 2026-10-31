using Microsoft.EntityFrameworkCore;
using Minto.Api.Data;
using System.Security.Claims;

namespace Minto.Api.Commerce.Orders;

/// <summary><c>GET /api/v1/commerce/orders</c> — lists a customer's orders.</summary>
public static class ListOrders
{
    /// <summary>Maps the customer order-list route.</summary>
    public static IEndpointRouteBuilder MapListOrders(this IEndpointRouteBuilder group)
    {
        group.MapGet("", HandleAsync).WithName("ListOrders").Produces<OrderResponse[]>();
        return group;
    }

    private static async Task<IResult> HandleAsync(ClaimsPrincipal user, MintoDbContext db, CancellationToken ct) =>
        Results.Ok((await db.Orders.Include(order => order.Lines).Where(order => order.CustomerId == user.FindFirstValue(ClaimTypes.NameIdentifier))
            .OrderByDescending(order => order.CreatedAt).ToArrayAsync(ct)).Select(OrderResponse.From).ToArray());
}

/// <summary>An order response.</summary>
public sealed record OrderResponse(Guid Id, string CustomerId, string Address, Minto.Api.Domain.OrderStatus Status,
    string PaymentMethod, bool PaymentCompleted, decimal Total, DateTimeOffset CreatedAt,
    string? StoreId, string? PickerId, string? RiderId, string? PickupCode, DateTimeOffset? DeliveryDeadline, bool IsPriority, OrderLineResponse[] Lines)
{
    public static OrderResponse From(Minto.Api.Domain.Order order) => new(order.Id, order.CustomerId, order.Address, order.Status,
        order.PaymentMethod, order.PaymentCompleted, order.Total, order.CreatedAt, order.StoreId, order.PickerId, order.RiderId, order.PickupCode, order.DeliveryDeadline, order.IsPriority,
        order.Lines.Select(line => new OrderLineResponse(line.ProductId, line.ProductName, line.Quantity, line.UnitPrice)).ToArray());
}

/// <summary>An order line response.</summary>
public sealed record OrderLineResponse(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);
