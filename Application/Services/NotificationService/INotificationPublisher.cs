namespace Application.Services.NotificationService
{
    public interface INotificationPublisher
    {
        Task PublishToUserAsync(int userId, string message, string sender);

        Task PublishToRoleAsync(string role, string message, string sender);
    }
}
