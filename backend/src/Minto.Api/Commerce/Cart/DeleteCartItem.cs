using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Minto.Api.Data;

namespace Minto.Api.Commerce.Cart;

/// <summary><c>DELETE /api/v1/commerce/cart/items/{productId}</c> — removes a cart item.</summary>
public static class DeleteCartItem
{
    /// <summary>Maps the cart item removal mutation.</summary>
    public static IEndpointRouteBuilder MapDeleteCartItem(this IEndpointRouteBuilder group)
    {
        group.MapDelete("/items/{productId:guid}", HandleAsync).WithName("DeleteCartItem").Produces<CartResponse>(); return group;
    }

    private static async Task<IResult> HandleAsync(Guid productId, ClaimsPrincipal user, MintoDbContext db, CancellationToken ct)
    {
        var customerId = user.FindFirstValue(ClaimTypes.NameIdentifier)!; var cart = await db.Carts.Include(item => item.Lines).SingleOrDefaultAsync(item => item.CustomerId == customerId, ct);
        var line = cart?.Lines.SingleOrDefault(item => item.ProductId == productId); if (line is not null) { db.CartLines.Remove(line); await db.SaveChangesAsync(ct); }
        return Results.Ok(await CartResponse.FromAsync(cart, db, ct));
    }
}
