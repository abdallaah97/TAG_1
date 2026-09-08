using Domain.Entities;

namespace Application.Repositories
{
    public interface IUnitOfWork
    {
        IGenericRepository<User> Users { get; }
        IGenericRepository<Role> Roles { get; }
        IGenericRepository<Permission> Permissions { get; }
        IGenericRepository<UserRole> UserRoles { get; }
        IGenericRepository<RolePermission> RolePermissions { get; }
        IGenericRepository<RefreshToken> RefreshTokens { get; }

        IGenericRepository<T> Repository<T>() where T : class;

        int SaveChanges();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
