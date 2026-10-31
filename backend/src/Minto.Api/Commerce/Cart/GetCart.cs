using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Minto.Api.Data;
using Minto.Api.Domain;

namespace Minto.Api.Commerce.Cart;

/// <summary><c>GET /api/v1/commerce/cart</c> — returns the authenticated customer's cart.</summary>
public static class GetCart
{
    /// <summary>Maps the cart query.</summary>
    public static IEndpointRouteBuilder MapGetCart(this IEndpointRouteBuilder group)
    {
        group.MapGet("", HandleAsync).WithName("GetCart").Produces<CartResponse>(); return group;
    }

    private static async Task<IResult> HandleAsync(ClaimsPrincipal user, MintoDbContext db, CancellationToken ct)
    {
        var customerId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var cart = await db.Carts.Include(item => item.Lines).SingleOrDefaultAsync(item => item.CustomerId == customerId, ct);
        return Results.Ok(await CartResponse.FromAsync(cart, db, ct));
    }
}

/// <summary>Cart response with authoritative product prices.</summary>
public sealed record CartResponse(Guid? Id, CartItemResponse[] Items, decimal Subtotal)
{
    public static async Task<CartResponse> FromAsync(Minto.Api.Domain.Cart? cart, MintoDbContext db, CancellationToken ct)
    {
        if (cart is null) return new(null, [], 0);
        var products = await db.Products.Where(product => cart.Lines.Select(line => line.ProductId).Contains(product.Id)).ToDictionaryAsync(product => product.Id, ct);
        var items = cart.Lines.Where(line => products.ContainsKey(line.ProductId)).Select(line => new CartItemResponse(line.ProductId, products[line.ProductId].Name, products[line.ProductId].Category, products[line.ProductId].Price, line.Quantity, products[line.ProductId].IsActive)).ToArray();
        return new(cart.Id, items, items.Sum(item => item.UnitPrice * item.Quantity));
    }
}

/// <summary>A persisted cart line.</summary>
public sealed record CartItemResponse(Guid ProductId, string ProductName, string Category, decimal UnitPrice, int Quantity, bool Available);
