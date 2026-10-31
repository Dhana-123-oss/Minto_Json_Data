using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Minto.Api.Commerce.Cart;
using Minto.Api.Data;
using Minto.Api.Domain;
using Xunit;

namespace Minto.Api.Tests;

public sealed class CartMutationTests
{
    [Fact]
    public async Task New_cart_can_receive_first_item_without_concurrency_failure()
    {
        await using var db = NewDb();
        var product = new Product { Name = "Milk", Category = "Dairy", Price = 28m, IsActive = true };
        db.Products.Add(product); await db.SaveChangesAsync();

        var cart = new Cart { CustomerId = "customer-1" };
        db.Carts.Add(cart); await db.SaveChangesAsync();
        db.CartLines.Add(new CartLine { CartId = cart.Id, ProductId = product.Id, Quantity = 2 });
        await db.SaveChangesAsync();

        Assert.Equal(2, await db.CartLines.Where(x => x.CartId == cart.Id).Select(x => x.Quantity).SingleAsync());
    }

    private static MintoDbContext NewDb() => new(new DbContextOptionsBuilder<MintoDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
