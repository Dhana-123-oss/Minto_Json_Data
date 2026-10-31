namespace Minto.Api.Operations.Picker;

/// <summary>Picker workflow routes.</summary>
public static class PickerEndpoints
{
    /// <summary>Maps picker queue operations.</summary>
    public static IEndpointRouteBuilder MapPickerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/operations/picker").WithTags("Picker").RequireAuthorization("Picker"); group.MapListPickerQueue(); group.MapTransitionOperation(); return app;
    }
}
