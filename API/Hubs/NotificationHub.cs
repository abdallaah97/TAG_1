using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var roles = Context.User?.FindAll("role").Select(c => c.Value).Distinct().ToList() ?? [];

            foreach (var role in roles)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, role);
            }

            await Clients.Caller.SendAsync("Connected", new
            {
                userId = Context.UserIdentifier,
                connectionId = Context.ConnectionId,
                groups = roles
            });

            await base.OnConnectedAsync();
        }
    }
}
