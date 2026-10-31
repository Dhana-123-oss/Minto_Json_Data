using Microsoft.EntityFrameworkCore;
using Minto.Api.Data;

namespace Minto.Api.Commerce.Products;

/// <summary><c>GET /api/v1/commerce/products</c> — lists active products.</summary>
public static class ListProducts
{
    /// <summary>Maps the product-list route.</summary>
    public static IEndpointRouteBuilder MapListProducts(this IEndpointRouteBuilder group)
    {
        group.MapGet("", HandleAsync).WithName("ListProducts").Produces<ProductResponse[]>();
        return group;
    }

    private static async Task<IResult> HandleAsync(MintoDbContext db, CancellationToken ct) =>
        Results.Ok(await db.Products.Where(product => product.IsActive).OrderBy(product => product.Category)
            .Select(product => new ProductResponse(product.Id, product.Name, product.Category, product.Price)).ToArrayAsync(ct));
}

/// <summary>A product returned by the catalog contract.</summary>
public sealed record ProductResponse(Guid Id, string Name, string Category, decimal Price);
