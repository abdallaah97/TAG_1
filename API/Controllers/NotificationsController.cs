using Application.Services.CurrentUserService;
using Application.Services.NotificationService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationPublisher _notificationPublisher;
        private readonly ICurrentUserService _currentUserService;

        public NotificationsController(INotificationPublisher notificationPublisher, ICurrentUserService currentUserService)
        {
            _notificationPublisher = notificationPublisher;
            _currentUserService = currentUserService;
        }

        [HttpPost("SendToUser/{userId:int}")]
        public async Task<IActionResult> SendToUser(int userId, [FromBody] SendNotificationDto input)
        {
            await _notificationPublisher.PublishToUserAsync(userId, input.Message, Sender);
            return Ok();
        }

        [HttpPost("SendToRole/{role}")]
        public async Task<IActionResult> SendToRole(string role, [FromBody] SendNotificationDto input)
        {
            await _notificationPublisher.PublishToRoleAsync(role, input.Message, Sender);
            return Ok();
        }

        private string Sender => _currentUserService.Name ?? "System";
    }

    public class SendNotificationDto
    {
        public string Message { get; set; } = string.Empty;
    }
}
