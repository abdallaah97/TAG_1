using Microsoft.AspNetCore.SignalR;

namespace API.Hubs
{
    public class NotificationUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst("id")?.Value;
        }
    }
}
