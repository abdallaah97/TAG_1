using Application.Common.Authorization;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    // Migration seed data: the permission catalog is code, the table is only a mirror of it.
    // Adding a permission to Permissions.All and creating a migration is all it takes.
    public static class PermissionSeedData
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            var permissions = new List<Permission>();
            foreach (var permission in Permissions.All)
            {
                permissions.Add(new Permission
                {
                    Id = permission.Id,
                    Name = permission.Name,
                    DisplayName = permission.DisplayName,
                    Group = permission.Group,
                    CreatedAt = SeedConstants.SeedDate
                });
            }
           

            modelBuilder.Entity<Permission>().HasData(permissions);
        }
    }
}
