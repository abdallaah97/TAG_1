using Application.Repositories;
using Application.Services.NotificationService;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.DemoUserSeederService
{
    public class DemoUserSeederService : IDemoUserSeederService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly INotificationPublisher _notificationPublisher;

        public DemoUserSeederService(
            IUnitOfWork unitOfWork,
            IPasswordHasher<User> passwordHasher,
            INotificationPublisher notificationPublisher)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _notificationPublisher = notificationPublisher;
        }

        public async Task AddRandomUserAsync()
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var role = await PickNextRoleAsync();
            var roleName = role?.Name ?? "User";

            var user = new User
            {
                Name = $"Demo {roleName} {suffix}",
                Email = $"demo.{roleName.ToLowerInvariant()}.{suffix}@tag.local",
                PhoneNumber = "0700000000",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            user.Password = _passwordHasher.HashPassword(user, "Demo@123");

            if (role != null)
            {
                user.UserRoles = [new UserRole { RoleId = role.Id, AssignedAt = DateTime.UtcNow }];
            }

            await _unitOfWork.Users.InsertAsync(user);
            await _unitOfWork.SaveChangesAsync();

            var message = $"New user created: {user.Name} (id {user.Id}) with role {roleName}";

            await _notificationPublisher.PublishToRoleAsync(SystemRoles.SuperAdmin, message, "System");
            await _notificationPublisher.PublishToRoleAsync(SystemRoles.Admin, message, "System");
        }

        private async Task<Role?> PickNextRoleAsync()
        {
            var roles = await _unitOfWork.Roles.GetAllReadOnly()
                .OrderBy(r => r.Id)
                .ToListAsync();

            if (roles.Count == 0)
            {
                return null;
            }

            var assignedCount = await _unitOfWork.UserRoles.GetAllReadOnly().CountAsync();

            return roles[assignedCount % roles.Count];
        }
    }
}
