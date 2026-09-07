using Domain.Entities;
using Infrastructure.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data
{
    // Runtime seed data: the first super admin. A password hash is not deterministic,
    // so this can not live inside a migration.
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseSeeder));

            await context.Database.MigrateAsync();

            var email = configuration["Seed:SuperAdmin:Email"] ?? "admin@tag.com";
            var password = configuration["Seed:SuperAdmin:Password"] ?? "Admin@123";
            var name = configuration["Seed:SuperAdmin:Name"] ?? "Super Admin";
            var phoneNumber = configuration["Seed:SuperAdmin:PhoneNumber"] ?? string.Empty;

            var superAdminExists = await context.UserRoles
                .AnyAsync(ur => ur.RoleId == SeedConstants.SuperAdminRoleId);

            if (superAdminExists)
            {
                return;
            }

            var user = await context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (user == null)
            {
                user = new User
                {
                    Name = name,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                user.Password = passwordHasher.HashPassword(user, password);

                context.Users.Add(user);
                await context.SaveChangesAsync();
            }

            context.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = SeedConstants.SuperAdminRoleId,
                AssignedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            logger.LogInformation("Seeded the super admin account {Email}.", email);
        }
    }
}
