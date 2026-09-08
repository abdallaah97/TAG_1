using Application.Repositories;
using Domain.Entities;
using Infrastructure.Context;

namespace Infrastructure.Repositories
{
    // Scoped, exactly like the AppDbContext it wraps: one instance per request, so every
    // repository it hands out shares the same change tracker.
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly Dictionary<Type, object> _repositories = new();

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public IGenericRepository<User> Users => Repository<User>();
        public IGenericRepository<Role> Roles => Repository<Role>();
        public IGenericRepository<Permission> Permissions => Repository<Permission>();
        public IGenericRepository<UserRole> UserRoles => Repository<UserRole>();
        public IGenericRepository<RolePermission> RolePermissions => Repository<RolePermission>();
        public IGenericRepository<RefreshToken> RefreshTokens => Repository<RefreshToken>();

        public IGenericRepository<T> Repository<T>() where T : class
        {
            if (_repositories.TryGetValue(typeof(T), out var existing))
            {
                return (IGenericRepository<T>)existing;
            }

            var repository = new GenericRepository<T>(_context);
            _repositories[typeof(T)] = repository;

            return repository;
        }

        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
