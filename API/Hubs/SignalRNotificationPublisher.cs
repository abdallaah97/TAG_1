using Application.Services.NotificationService;
using Microsoft.AspNetCore.SignalR;

namespace API.Hubs
{
    public class SignalRNotificationPublisher : INotificationPublisher
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public SignalRNotificationPublisher(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task PublishToUserAsync(int userId, string message, string sender)
        {
            return _hubContext.Clients
                .User(userId.ToString())
                .SendAsync("ReceiveNotification", BuildPayload($"User #{userId}", message, sender));
        }

        public Task PublishToRoleAsync(string role, string message, string sender)
        {
            return _hubContext.Clients
                .Group(role)
                .SendAsync("ReceiveNotification", BuildPayload($"Role: {role}", message, sender));
        }

        private static object BuildPayload(string target, string message, string sender)
        {
            return new
            {
                target,
                message,
                sender,
                sentAt = DateTime.UtcNow
            };
        }
    }
}
