using Microsoft.AspNetCore.SignalR;
namespace Minto.Api.Features;
public sealed class TrackingHub : Hub
{
    public Task SubscribeToOrder(Guid orderId) => Groups.AddToGroupAsync(Context.ConnectionId, orderId.ToString());
}
