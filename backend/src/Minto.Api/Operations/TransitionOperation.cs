using System.Security.Claims;
using MediatR;
using Minto.Api.Commerce.Orders;
using Minto.Api.Domain;
using Minto.Api.Features;

namespace Minto.Api.Operations;

/// <summary>Role-scoped operational state changes for picker and rider workflows.</summary>
public static class TransitionOperation
{
    public static IEndpointRouteBuilder MapTransitionOperation(this IEndpointRouteBuilder group)
    {
        group.MapPost("/orders/{id:guid}/transition", HandleAsync)
            .Produces<OrderResponse>()
            .ProducesProblem(400).ProducesProblem(403).ProducesProblem(404);
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid id, TransitionOrderRequest request,
        ClaimsPrincipal user, IMediator mediator, CancellationToken ct)
    {
        var role = user.FindFirstValue(ClaimTypes.Role);
        var allowed = role switch
        {
            "Picker" => request.Status is OrderStatus.Picking or OrderStatus.Picked or OrderStatus.Packed or OrderStatus.ReadyForPickup,
            "Rider" => request.Status is OrderStatus.Assigned or OrderStatus.Accepted or OrderStatus.OutForDelivery or OrderStatus.Delivered,
            "Admin" => true,
            _ => false
        };
        if (!allowed) return Results.Forbid();
        var actor = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        return Results.Ok(OrderResponse.From(await mediator.Send(
            new TransitionOrderCommand(id, request.Status, actor, request.PickupCode), ct)));
    }
}
