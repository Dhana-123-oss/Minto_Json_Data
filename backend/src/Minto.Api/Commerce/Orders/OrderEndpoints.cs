namespace Minto.Api.Commerce.Orders;

/// <summary>All order routes.</summary>
public static class OrderEndpoints
{
    /// <summary>Maps the order group and its contract operations.</summary>
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/commerce/orders").WithTags("Orders").RequireAuthorization("Customer");
        group.MapListOrders();
        group.MapGetOrder();
        group.MapCreateOrder();
        group.MapTransitionOrder();
        return app;
    }
}
