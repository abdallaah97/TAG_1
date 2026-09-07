using Application.Common.Authorization;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    // Migration seed data for the three roles the application ships with.
    // SuperAdmin holds every permission, Admin manages users, User is an empty starting point.
    public static class RoleSeedData
    {
        private static readonly string[] AdminPermissions =
        {
            Permissions.Users.View,
            Permissions.Users.Create,
            Permissions.Users.Update,
            Permissions.Users.ChangePassword,
            Permissions.Users.AssignRoles,
            Permissions.Roles.View
        };

        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>().HasData(
                new Role
                {
                    Id = SeedConstants.SuperAdminRoleId,
                    Name = SystemRoles.SuperAdmin,
                    NormalizedName = SystemRoles.SuperAdmin.ToUpperInvariant(),
                    Description = "Full access to every part of the system.",
                    IsSystemRole = true,
                    CreatedAt = SeedConstants.SeedDate
                },
                new Role
                {
                    Id = SeedConstants.AdminRoleId,
                    Name = SystemRoles.Admin,
                    NormalizedName = SystemRoles.Admin.ToUpperInvariant(),
                    Description = "Manages users and reads roles.",
                    IsSystemRole = true,
                    CreatedAt = SeedConstants.SeedDate
                },
                new Role
                {
                    Id = SeedConstants.UserRoleId,
                    Name = SystemRoles.User,
                    NormalizedName = SystemRoles.User.ToUpperInvariant(),
                    Description = "A regular account without administrative rights.",
                    IsSystemRole = true,
                    CreatedAt = SeedConstants.SeedDate
                }
            );

            var rolePermissions = new List<RolePermission>();

            foreach (var permission in Permissions.All)
            {
                rolePermissions.Add(new RolePermission
                {
                    RoleId = SeedConstants.SuperAdminRoleId,
                    PermissionId = permission.Id,
                    GrantedAt = SeedConstants.SeedDate
                });

                if (AdminPermissions.Contains(permission.Name))
                {
                    rolePermissions.Add(new RolePermission
                    {
                        RoleId = SeedConstants.AdminRoleId,
                        PermissionId = permission.Id,
                        GrantedAt = SeedConstants.SeedDate
                    });
                }
            }

            modelBuilder.Entity<RolePermission>().HasData(rolePermissions);
        }
    }
}
