using Application.Services.CurrentUserService;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using PermissionCatalog = Application.Common.Authorization.Permissions;

namespace Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public int? UserId
        {
            get
            {
                var userId = User?.FindFirst("id")?.Value;
                return int.TryParse(userId, out var id) ? id : null;
            }
        }

        public string? Name => User?.FindFirst("name")?.Value;

        public string? Email => User?.FindFirst("email")?.Value;

        public string? PhoneNumber => User?.FindFirst("phoneNumber")?.Value;

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

        public IReadOnlyList<string> Roles =>
            User?.FindAll("role").Select(c => c.Value).ToList() ?? new List<string>();

        public IReadOnlyList<string> Permissions =>
            User?.FindAll(PermissionCatalog.ClaimType).Select(c => c.Value).ToList() ?? new List<string>();

        public string? IpAddress
        {
            get
            {
                var context = _httpContextAccessor.HttpContext;
                if (context == null)
                {
                    return null;
                }

                // Behind a reverse proxy the original caller is the first entry of the header.
                if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
                {
                    var value = forwarded.ToString().Split(',').FirstOrDefault()?.Trim();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }

                return context.Connection.RemoteIpAddress?.ToString();
            }
        }
    }
}
