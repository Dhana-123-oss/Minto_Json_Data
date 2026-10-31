using Minto.Api.Commerce.Cart;

namespace Minto.Api.Commerce.Cart;

/// <summary>Authenticated customer cart routes.</summary>
public static class CartEndpoints
{
    /// <summary>Maps cart query and mutation operations.</summary>
    public static IEndpointRouteBuilder MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/commerce/cart").WithTags("Cart").RequireAuthorization("Customer");
        group.MapGetCart(); group.MapPutCartItem(); group.MapDeleteCartItem(); return app;
    }
}
