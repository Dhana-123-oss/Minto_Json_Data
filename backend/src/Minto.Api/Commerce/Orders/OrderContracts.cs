using Minto.Api.Domain;

namespace Minto.Api.Commerce.Orders;

/// <summary>The body of <c>POST /api/v1/commerce/orders</c>.</summary>
public sealed record CreateOrderRequest(string Address, string PaymentMethod, List<CreateOrderLine> Lines);

/// <summary>A requested order line.</summary>
public sealed record CreateOrderLine(Guid ProductId, int Quantity);

/// <summary>The body of the order state transition operation.</summary>
public sealed record TransitionOrderRequest(OrderStatus Status, string ActorId, string? PickupCode);
