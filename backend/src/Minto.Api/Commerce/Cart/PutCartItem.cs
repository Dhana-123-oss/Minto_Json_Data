using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Minto.Api.Data;
using Minto.Api.Domain;

namespace Minto.Api.Commerce.Cart;

/// <summary><c>PUT /api/v1/commerce/cart/items/{productId}</c> — sets a cart quantity.</summary>
public static class PutCartItem
{
    /// <summary>Maps the cart item quantity mutation.</summary>
    public static IEndpointRouteBuilder MapPutCartItem(this IEndpointRouteBuilder group)
    {
        group.MapPut("/items/{productId:guid}", HandleAsync).WithName("PutCartItem").Produces<CartResponse>().ProducesProblem(404).ProducesProblem(409); return group;
    }

    private static async Task<IResult> HandleAsync(Guid productId, CartQuantityRequest request, ClaimsPrincipal user, MintoDbContext db, CancellationToken ct)
    {
        if (request.Quantity < 1) return Results.ValidationProblem(new Dictionary<string, string[]> { ["quantity"] = ["Quantity must be at least 1."] });
        var product = await db.Products.SingleOrDefaultAsync(item => item.Id == productId && item.IsActive, ct);
        if (product is null) return Results.NotFound(new { message = "Product is unavailable." });
        var customerId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var cart = await db.Carts.Include(item => item.Lines).SingleOrDefaultAsync(item => item.CustomerId == customerId, ct);
        if (cart is null)
        {
            cart = new Minto.Api.Domain.Cart { CustomerId = customerId };
            db.Carts.Add(cart);
            await db.SaveChangesAsync(ct);
        }

        var line = cart.Lines.SingleOrDefault(item => item.ProductId == productId);
        if (line is null)
        {
            db.CartLines.Add(new CartLine { CartId = cart.Id, ProductId = productId, Quantity = request.Quantity });
        }
        else
        {
            line.Quantity = request.Quantity;
        }
        cart.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(await CartResponse.FromAsync(cart, db, ct));
    }
}

/// <summary>Sets a persisted cart quantity.</summary>
public sealed record CartQuantityRequest(int Quantity);
