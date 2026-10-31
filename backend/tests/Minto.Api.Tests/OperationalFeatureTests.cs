using Microsoft.EntityFrameworkCore;
using Minto.Api.Data;
using Minto.Api.Domain;
using Xunit;

namespace Minto.Api.Tests;

public sealed class OperationalFeatureTests
{
    [Fact]
    public async Task Operational_entities_persist_for_timeline_notifications_and_assignments()
    {
        await using var db = new MintoDbContext(new DbContextOptionsBuilder<MintoDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var order = new Order { CustomerId = "customer-1", Address = "Minto Street", Lines = [], PickerId = "picker-1", IsPriority = true, DeliveryDeadline = DateTimeOffset.UtcNow.AddMinutes(15) };
        db.Orders.Add(order);
        db.OrderEvents.Add(new OrderEvent { OrderId = order.Id, Status = "Picking", ActorId = "picker-1" });
        db.Notifications.Add(new Notification { UserId = "customer-1", Title = "Order update", Message = "Picking", Type = "OrderStatus" });
        db.StaffAssignments.Add(new StaffAssignment { StaffId = "picker-1", Role = "Picker", StoreId = "store-1" });
        await db.SaveChangesAsync();
        Assert.Contains(db.OrderEvents, x => x.OrderId == order.Id && x.Status == "Picking");
        Assert.Contains(db.Notifications, x => x.UserId == "customer-1" && x.Type == "OrderStatus");
        Assert.True((await db.Orders.SingleAsync()).IsPriority);
    }

    [Fact]
    public async Task Customer_wishlist_items_persist_per_customer_and_product()
    {
        await using var db = new MintoDbContext(new DbContextOptionsBuilder<MintoDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var product = new Product { Name = "Bananas", Category = "Fresh produce", Price = 42m, IsActive = true };
        db.Products.Add(product);
        db.WishlistItems.Add(new WishlistItem { CustomerId = "customer-1", ProductId = product.Id });
        await db.SaveChangesAsync();

        Assert.Single(await db.WishlistItems.Where(x => x.CustomerId == "customer-1" && x.ProductId == product.Id).ToListAsync());
    }
}
