namespace Minto.Api.Commerce.Products;

/// <summary>All product routes.</summary>
public static class ProductEndpoints
{
    /// <summary>Maps the product catalog group and its contract operations.</summary>
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/commerce/products").WithTags("Products").RequireAuthorization("Customer");
        group.MapListProducts();
        return app;
    }
}
