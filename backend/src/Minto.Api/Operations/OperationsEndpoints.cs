using Minto.Api.Operations.Admin;
using Minto.Api.Operations.Picker;
using Minto.Api.Operations.Rider;

namespace Minto.Api.Operations;

/// <summary>Operational APIs for store, picker, rider, and admin workflows.</summary>
public static class OperationsEndpoints
{
    /// <summary>Maps operational route groups.</summary>
    public static IEndpointRouteBuilder MapOperationsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPickerEndpoints(); app.MapRiderEndpoints(); app.MapAdminEndpoints(); return app;
    }
}
