using Minto.Api.Commerce.Orders;
using Minto.Api.Commerce.Products;
using Minto.Api.Commerce.Cart;

namespace Minto.Api.Commerce;

/// <summary>All commerce routes.</summary>
public static class CommerceEndpoints
{
    /// <summary>Maps the commerce groups and their contract operations.</summary>
    public static IEndpointRouteBuilder MapCommerceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapCartEndpoints();
        app.MapProductEndpoints();
        app.MapOrderEndpoints();
        return app;
    }
}
