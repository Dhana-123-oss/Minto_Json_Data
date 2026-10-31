using MediatR;
using Microsoft.AspNetCore.Mvc;
using Minto.Api.Domain;
using Minto.Api.Features;
using System.Security.Claims;

namespace Minto.Api.Commerce.Orders;

/// <summary><c>POST /api/v1/commerce/orders</c> — creates an order and reserves inventory.</summary>
public static class CreateOrder
{
    /// <summary>Maps the order-create route.</summary>
    public static IEndpointRouteBuilder MapCreateOrder(this IEndpointRouteBuilder group)
    {
        group.MapPost("", HandleAsync).WithName("CreateOrder").Produces<OrderResponse>(StatusCodes.Status201Created)
            .ProducesProblem(400).ProducesProblem(409);
        return group;
    }

    private static async Task<IResult> HandleAsync(CreateOrderRequest request, IMediator mediator, ClaimsPrincipal user,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken ct)
    {
        var customerId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var order = await mediator.Send(new CreateOrderCommand(request, customerId, idempotencyKey), ct);
        return Results.Created($"/api/v1/commerce/orders/{order.Id}", OrderResponse.From(order));
    }
}

/// <summary>Creates an order through the commerce command pipeline.</summary>
public sealed record CreateOrderCommand(CreateOrderRequest Request, string CustomerId, string? IdempotencyKey) : IRequest<Order>;
