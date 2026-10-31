using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Minto.Api.Commerce.Orders;
using Minto.Api.Data;
using Minto.Api.Domain;
using Microsoft.AspNetCore.SignalR;

namespace Minto.Api.Features;

/// <summary>Creates orders and reserves inventory atomically.</summary>
public sealed class CreateOrderHandler(MintoDbContext db) : IRequestHandler<CreateOrderCommand, Order>
{
    /// <inheritdoc />
    public async Task<Order> Handle(CreateOrderCommand command, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(command.IdempotencyKey) && await db.Orders.Include(order => order.Lines)
            .FirstOrDefaultAsync(order => order.IdempotencyKey == command.IdempotencyKey, ct) is { } existing) return existing;
        if (command.Request.Lines.Count == 0) throw new ArgumentException("At least one line is required");
        var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
        var order = new Order { CustomerId = command.CustomerId, Address = command.Request.Address, DeliveryOtp = Random.Shared.Next(100000, 999999).ToString(),
            PaymentMethod = command.Request.PaymentMethod, IdempotencyKey = command.IdempotencyKey };
        foreach (var line in command.Request.Lines)
        {
            if (line.Quantity < 1) throw new ArgumentException("Quantity must be positive");
            var product = await db.Products.FindAsync([line.ProductId], ct) ?? throw new KeyNotFoundException("Product not found");
            var stock = await db.Inventory.SingleAsync(item => item.ProductId == product.Id, ct);
            if (stock.Available < line.Quantity) throw new InvalidOperationException($"Insufficient inventory for {product.Name}");
            stock.Available -= line.Quantity; stock.Reserved += line.Quantity;
            order.Lines.Add(new OrderLine { OrderId = order.Id, ProductId = product.Id, ProductName = product.Name,
                Quantity = line.Quantity, UnitPrice = product.Price });
            order.Total += product.Price * line.Quantity;
        }
        db.Orders.Add(order);
        var cart = await db.Carts.Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.CustomerId == command.CustomerId, ct);
        if (cart is not null)
        {
            db.CartLines.RemoveRange(cart.Lines);
            cart.UpdatedAt = DateTimeOffset.UtcNow;
        }
        db.Outbox.Add(new OutboxMessage { EventType = "OrderPlaced", Payload = JsonSerializer.Serialize(new { orderId = order.Id, order.Total }) });
        await db.SaveChangesAsync(ct);
        if (transaction is not null) { await transaction.CommitAsync(ct); await transaction.DisposeAsync(); }
        return order;
    }
}

/// <summary>Advances orders only through allowed domain transitions.</summary>
public sealed class TransitionOrderHandler(MintoDbContext db, IHubContext<TrackingHub> hub) : IRequestHandler<TransitionOrderCommand, Order>
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Allowed = new()
    {
        [OrderStatus.Placed] = [OrderStatus.Picking], [OrderStatus.Picking] = [OrderStatus.Picked],
        [OrderStatus.Picked] = [OrderStatus.Packed], [OrderStatus.Packed] = [OrderStatus.ReadyForPickup],
        [OrderStatus.ReadyForPickup] = [OrderStatus.Assigned], [OrderStatus.Assigned] = [OrderStatus.Accepted],
        [OrderStatus.Accepted] = [OrderStatus.OutForDelivery], [OrderStatus.OutForDelivery] = [OrderStatus.Delivered]
    };

    /// <inheritdoc />
    public async Task<Order> Handle(TransitionOrderCommand command, CancellationToken ct)
    {
        var order = await db.Orders.Include(item => item.Lines).SingleOrDefaultAsync(item => item.Id == command.Id, ct)
            ?? throw new KeyNotFoundException("Order not found");
        if (!Allowed.TryGetValue(order.Status, out var next) || !next.Contains(command.Status))
            throw new InvalidOperationException($"Invalid transition {order.Status} -> {command.Status}");
        if (command.Status is OrderStatus.Assigned or OrderStatus.Accepted) order.RiderId = command.ActorId;
        if (command.Status == OrderStatus.OutForDelivery && order.PickupCode is not null && order.PickupCode != command.PickupCode)
            throw new InvalidOperationException("Pickup verification failed");
        if (command.Status == OrderStatus.ReadyForPickup) order.PickupCode = Random.Shared.Next(100000, 999999).ToString();
        order.Status = command.Status;
        if (command.Status == OrderStatus.Delivered && order.PaymentMethod == "COD") order.PaymentCompleted = true;
        db.OrderEvents.Add(new OrderEvent { OrderId = order.Id, Status = order.Status.ToString(), ActorId = command.ActorId });
        db.Notifications.Add(new Notification { UserId = order.CustomerId, Title = "Order update", Message = $"Your order is now {order.Status}.", Type = "OrderStatus" });
        db.Outbox.Add(new OutboxMessage { EventType = "OrderStatusChanged", Payload = JsonSerializer.Serialize(new { orderId = order.Id, status = order.Status.ToString(), actorId = command.ActorId }) });
        await db.SaveChangesAsync(ct);
        await hub.Clients.Group(order.Id.ToString()).SendAsync("order.updated", new { orderId = order.Id, status = order.Status.ToString() }, ct);
        return order;
    }
}
