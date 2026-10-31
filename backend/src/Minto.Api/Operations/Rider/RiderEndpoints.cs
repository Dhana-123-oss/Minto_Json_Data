namespace Minto.Api.Operations.Rider;

/// <summary>Rider workflow routes.</summary>
public static class RiderEndpoints
{
    /// <summary>Maps rider queue operations.</summary>
    public static IEndpointRouteBuilder MapRiderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/operations/rider").WithTags("Rider").RequireAuthorization("Rider"); group.MapListRiderQueue(); group.MapTransitionOperation(); return app;
    }
}
