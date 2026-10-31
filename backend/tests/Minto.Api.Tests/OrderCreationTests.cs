using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Minto.Api.Commerce.Orders;
using Minto.Api.Data;
using Minto.Api.Domain;
using Minto.Api.Features;
using Xunit;

namespace Minto.Api.Tests;

public sealed class OrderCreationTests
{
    [Fact]
    public async Task Creating_order_reserves_stock_and_clears_customer_cart()
    {
        await using var db = NewDb();
        var product = new Product { Name = "Tomato", Category = "Fresh produce", Price = 20m, IsActive = true };
        db.Products.Add(product);
        db.Inventory.Add(new InventoryItem { ProductId = product.Id, Available = 5 });
        var cart = new Cart { CustomerId = "customer-1" };
        cart.Lines.Add(new CartLine { CartId = cart.Id, ProductId = product.Id, Quantity = 2 });
        db.Carts.Add(cart);
        await db.SaveChangesAsync();

        var order = await new CreateOrderHandler(db).Handle(new CreateOrderCommand(
            new CreateOrderRequest("12 Minto Street", "COD", [new CreateOrderLine(product.Id, 2)]),
            "customer-1", "checkout-1"), CancellationToken.None);

        Assert.Equal(OrderStatus.Placed, order.Status);
        Assert.Equal(2, order.Lines.Single().Quantity);
        Assert.Equal(3, await db.Inventory.Where(x => x.ProductId == product.Id).Select(x => x.Available).SingleAsync());
        Assert.Empty(await db.CartLines.Where(x => x.CartId == cart.Id).ToListAsync());
    }

    [Fact]
    public async Task Same_idempotency_key_returns_existing_order_without_reserving_twice()
    {
        await using var db = NewDb();
        var product = new Product { Name = "Bread", Category = "Bakery", Price = 30m, IsActive = true };
        db.Products.Add(product);
        db.Inventory.Add(new InventoryItem { ProductId = product.Id, Available = 4 });
        await db.SaveChangesAsync();
        var command = new CreateOrderCommand(new CreateOrderRequest("12 Minto Street", "COD", [new CreateOrderLine(product.Id, 1)]), "customer-1", "same-key");

        var first = await new CreateOrderHandler(db).Handle(command, CancellationToken.None);
        var second = await new CreateOrderHandler(db).Handle(command, CancellationToken.None);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(3, await db.Inventory.Where(x => x.ProductId == product.Id).Select(x => x.Available).SingleAsync());
    }

    private static MintoDbContext NewDb() => new(new DbContextOptionsBuilder<MintoDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
