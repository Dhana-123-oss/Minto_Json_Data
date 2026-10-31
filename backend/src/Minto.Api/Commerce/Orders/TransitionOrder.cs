using MediatR;
using System.Security.Claims;
using Minto.Api.Domain;
using Minto.Api.Features;

namespace Minto.Api.Commerce.Orders;

/// <summary><c>POST /api/v1/commerce/orders/{id}/transition</c> — advances an order.</summary>
public static class TransitionOrder
{
    /// <summary>Maps the guarded transition route.</summary>
    public static IEndpointRouteBuilder MapTransitionOrder(this IEndpointRouteBuilder group)
    {
        group.MapPost("/{id:guid}/transition", HandleAsync).WithName("TransitionOrder").Produces<OrderResponse>()
            .ProducesProblem(400).ProducesProblem(404).ProducesProblem(409);
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid id, TransitionOrderRequest request, ClaimsPrincipal user, IMediator mediator,
        CancellationToken ct) => Results.Ok(OrderResponse.From(await mediator.Send(
            new TransitionOrderCommand(id, request.Status, user.FindFirstValue(ClaimTypes.NameIdentifier) ?? request.ActorId, request.PickupCode), ct)));
}

/// <summary>Advances an order through the domain state machine.</summary>
public sealed record TransitionOrderCommand(Guid Id, OrderStatus Status, string ActorId, string? PickupCode) : IRequest<Order>;
