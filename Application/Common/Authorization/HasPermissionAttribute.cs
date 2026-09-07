using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Application.Common.Authorization
{
    // Usage: [HasPermission(Permissions.Users.Create)]
    // Runs as an authorization filter: rejects unauthenticated callers, then allows
    // SuperAdmin or anyone carrying the matching "permission" claim in their access token.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class HasPermissionAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string _permission;

        public HasPermissionAttribute(string permission)
        {
            _permission = permission;
        }

        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (user.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedResult();
                return Task.CompletedTask;
            }

            var allowed = user.IsInRole(SystemRoles.SuperAdmin)
                       || user.FindAll(Permissions.ClaimType).Any(claim => string.Equals(claim.Value, _permission, StringComparison.OrdinalIgnoreCase));

            if (!allowed)
            {
                context.Result = new ForbidResult();
            }

            return Task.CompletedTask;
        }
    }
}
