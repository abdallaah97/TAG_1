using Application.Common.Exceptions;
using Application.Repositories;
using Application.Services.RoleService.DTOs;
using Application.Services.SecurityService;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.RoleService
{
    public class RoleService : IRoleService
    {
        private readonly IGenericRepository<Role> _roleRepository;
        private readonly IGenericRepository<Permission> _permissionRepository;
        private readonly IGenericRepository<RolePermission> _rolePermissionRepository;
        private readonly IGenericRepository<UserRole> _userRoleRepository;
        private readonly IUserSecurityService _userSecurityService;

        public RoleService(
            IGenericRepository<Role> roleRepository,
            IGenericRepository<Permission> permissionRepository,
            IGenericRepository<RolePermission> rolePermissionRepository,
            IGenericRepository<UserRole> userRoleRepository,
            IUserSecurityService userSecurityService)
        {
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
            _rolePermissionRepository = rolePermissionRepository;
            _userRoleRepository = userRoleRepository;
            _userSecurityService = userSecurityService;
        }

        public async Task<List<GetRoleDto>> GetAllRoles(GetAllRolesInputDto input)
        {
            var query = _roleRepository.GetAllReadOnly()
                .Include(x => x.RolePermissions)
                .ThenInclude(x => x.Permission).AsQueryable();

            if (!string.IsNullOrWhiteSpace(input.Search))
            {
                var search = input.Search.Trim();
                query = query.Where(r => r.Name.Contains(search));
            }

            return await query
                .OrderByDescending(r => r.IsSystemRole)
                .ThenBy(r => r.Name)
                .Select(r => new GetRoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    IsSystemRole = r.IsSystemRole,
                    CreatedAt = r.CreatedAt,
                    UsersCount = r.UserRoles.Count,
                    PermissionsCount = r.RolePermissions.Count,
                    Permissions = r.RolePermissions.Select(x => new PermissionDto
                    {
                        Id = x.PermissionId,
                        Name = x.Permission.Name,
                        DisplayName = x.Permission.DisplayName,
                        Group = x.Permission.Group
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<RoleDetailsDto> GetRoleById(int id)
        {
            var role = await _roleRepository.GetAllReadOnly()
                .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
                .Include(r => r.UserRoles)
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new NotFoundException("Role", id);

            return new RoleDetailsDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsSystemRole = role.IsSystemRole,
                CreatedAt = role.CreatedAt,
                UsersCount = role.UserRoles.Count,
                PermissionsCount = role.RolePermissions.Count,
                Permissions = role.RolePermissions
                    .Select(rp => MapPermission(rp.Permission))
                    .OrderBy(p => p.Group).ThenBy(p => p.Name)
                    .ToList()
            };
        }

        public async Task<int> CreateRole(CreateRoleDto input)
        {
            var name = input.Name.Trim();
            var normalizedName = name.ToUpperInvariant();

            var isNameTaken = await _roleRepository.GetAllReadOnly()
                .AnyAsync(r => r.NormalizedName == normalizedName);

            if (isNameTaken)
            {
                throw new ConflictException("A role with this name already exists");
            }

            var permissions = await LoadPermissionsAsync(input.Permissions);

            var role = new Role
            {
                Name = name,
                NormalizedName = normalizedName,
                Description = input.Description?.Trim(),
                IsSystemRole = false,
                CreatedAt = DateTime.UtcNow,
                RolePermissions = permissions
                    .Select(p => new RolePermission { PermissionId = p.Id, GrantedAt = DateTime.UtcNow })
                    .ToList()
            };

            await _roleRepository.InsertAsync(role);
            await _roleRepository.SaveChangesAsync();

            return role.Id;
        }

        public async Task UpdateRole(UpdateRoleDto input)
        {
            var role = await _roleRepository.GetByIdAsync(input.Id)
                ?? throw new NotFoundException("Role", input.Id);

            var name = input.Name.Trim();
            var normalizedName = name.ToUpperInvariant();

            // A system role is referenced by name from the code, so renaming it is not allowed.
            if (role.IsSystemRole && role.NormalizedName != normalizedName)
            {
                throw new BadRequestException("A system role can not be renamed");
            }

            var isNameTaken = await _roleRepository.GetAllReadOnly()
                .AnyAsync(r => r.NormalizedName == normalizedName && r.Id != input.Id);

            if (isNameTaken)
            {
                throw new ConflictException("A role with this name already exists");
            }

            role.Name = name;
            role.NormalizedName = normalizedName;
            role.Description = input.Description?.Trim();

            _roleRepository.Update(role);
            await _roleRepository.SaveChangesAsync();
        }

        public async Task DeleteRole(int id)
        {
            var role = await _roleRepository.GetAll()
                .Include(r => r.RolePermissions)
                .Include(r => r.UserRoles)
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new NotFoundException("Role", id);

            if (role.IsSystemRole)
            {
                throw new BadRequestException("A system role can not be deleted");
            }

            if (role.UserRoles.Count > 0)
            {
                throw new BadRequestException("This role is assigned to users, remove the assignments first");
            }

            _rolePermissionRepository.DeleteRange(role.RolePermissions);
            _roleRepository.Delete(role);
            await _roleRepository.SaveChangesAsync();
        }

        public async Task<List<PermissionGroupDto>> GetAllPermissions()
        {
            var permissions = await _permissionRepository.GetAllReadOnly()
                .OrderBy(p => p.Group).ThenBy(p => p.Id)
                .ToListAsync();

            return permissions
                .GroupBy(p => p.Group)
                .Select(g => new PermissionGroupDto
                {
                    Group = g.Key,
                    Permissions = g.Select(MapPermission).ToList()
                })
                .ToList();
        }

        public async Task<List<PermissionDto>> GetRolePermissions(int roleId)
        {
            await EnsureRoleExistsAsync(roleId);

            return await _rolePermissionRepository.GetAllReadOnly()
                .Where(rp => rp.RoleId == roleId)
                .OrderBy(rp => rp.Permission.Group).ThenBy(rp => rp.Permission.Id)
                .Select(rp => new PermissionDto
                {
                    Id = rp.Permission.Id,
                    Name = rp.Permission.Name,
                    DisplayName = rp.Permission.DisplayName,
                    Group = rp.Permission.Group
                })
                .ToListAsync();
        }

        public async Task UpdateRolePermissions(UpdateRolePermissionsDto input)
        {
            var role = await _roleRepository.GetAll()
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == input.RoleId)
                ?? throw new NotFoundException("Role", input.RoleId);

            if (role.Name == SystemRoles.SuperAdmin)
            {
                throw new BadRequestException("The super admin role always holds every permission and can not be changed");
            }

            var permissions = await LoadPermissionsAsync(input.Permissions);
            var requestedIds = permissions.Select(p => p.Id).ToHashSet();

            var removed = role.RolePermissions.Where(rp => !requestedIds.Contains(rp.PermissionId)).ToList();
            var addedIds = requestedIds.Except(role.RolePermissions.Select(rp => rp.PermissionId)).ToList();

            if (removed.Count == 0 && addedIds.Count == 0)
            {
                return;
            }

            _rolePermissionRepository.DeleteRange(removed);

            await _rolePermissionRepository.InsertRangeAsync(addedIds
                .Select(permissionId => new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permissionId,
                    GrantedAt = DateTime.UtcNow
                })
                .ToList());

            await RevokeSessionsOfRoleAsync(role.Id);

            await _rolePermissionRepository.SaveChangesAsync();
        }

        public async Task AddPermissionToRole(int roleId, string permission)
        {
            var role = await _roleRepository.GetByIdAsync(roleId)
                ?? throw new NotFoundException("Role", roleId);

            if (role.Name == SystemRoles.SuperAdmin)
            {
                throw new BadRequestException("The super admin role always holds every permission and can not be changed");
            }

            var entity = await FindPermissionAsync(permission);

            var alreadyGranted = await _rolePermissionRepository.GetAllReadOnly()
                .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == entity.Id);

            if (alreadyGranted)
            {
                throw new ConflictException("This permission is already granted to the role");
            }

            await _rolePermissionRepository.InsertAsync(new RolePermission
            {
                RoleId = roleId,
                PermissionId = entity.Id,
                GrantedAt = DateTime.UtcNow
            });

            await RevokeSessionsOfRoleAsync(roleId);

            await _rolePermissionRepository.SaveChangesAsync();
        }

        public async Task RemovePermissionFromRole(int roleId, string permission)
        {
            var role = await _roleRepository.GetByIdAsync(roleId)
                ?? throw new NotFoundException("Role", roleId);

            if (role.Name == SystemRoles.SuperAdmin)
            {
                throw new BadRequestException("The super admin role always holds every permission and can not be changed");
            }

            var entity = await FindPermissionAsync(permission);

            var rolePermission = await _rolePermissionRepository.GetAll()
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == entity.Id)
                ?? throw new NotFoundException("This permission is not granted to the role");

            _rolePermissionRepository.Delete(rolePermission);

            await RevokeSessionsOfRoleAsync(roleId);

            await _rolePermissionRepository.SaveChangesAsync();
        }

        public async Task<List<RoleUserDto>> GetRoleUsers(int roleId)
        {
            await EnsureRoleExistsAsync(roleId);

            return await _userRoleRepository.GetAllReadOnly()
                .Where(ur => ur.RoleId == roleId)
                .OrderBy(ur => ur.User.Name)
                .Select(ur => new RoleUserDto
                {
                    Id = ur.UserId,
                    Name = ur.User.Name,
                    Email = ur.User.Email,
                    IsActive = ur.User.IsActive
                })
                .ToListAsync();
        }

        // Everybody who holds the role gets a token that no longer matches what the role grants,
        // so their refresh tokens are dropped and the next refresh forces a fresh sign in.
        private async Task RevokeSessionsOfRoleAsync(int roleId)
        {
            var userIds = await _userRoleRepository.GetAllReadOnly()
                .Where(ur => ur.RoleId == roleId)
                .Select(ur => ur.UserId)
                .ToListAsync();

            await _userSecurityService.RevokeUsersTokensAsync(userIds, RevokeReasons.SecurityChanged, saveChanges: false);
        }

        private async Task EnsureRoleExistsAsync(int roleId)
        {
            var exists = await _roleRepository.GetAllReadOnly().AnyAsync(r => r.Id == roleId);
            if (!exists)
            {
                throw new NotFoundException("Role", roleId);
            }
        }

        private async Task<Permission> FindPermissionAsync(string permission)
        {
            var name = permission.Trim();

            return await _permissionRepository.GetAllReadOnly().FirstOrDefaultAsync(p => p.Name == name)
                ?? throw new NotFoundException($"Permission '{name}' does not exist");
        }

        private async Task<List<Permission>> LoadPermissionsAsync(List<string> permissionNames)
        {
            var names = permissionNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .Distinct()
                .ToList();

            if (names.Count == 0)
            {
                return new List<Permission>();
            }

            var permissions = await _permissionRepository.GetAllReadOnly()
                .Where(p => names.Contains(p.Name))
                .ToListAsync();

            var missing = names.Except(permissions.Select(p => p.Name)).ToList();
            if (missing.Count > 0)
            {
                throw new NotFoundException($"The following permissions do not exist: {string.Join(", ", missing)}");
            }

            return permissions;
        }

        private static PermissionDto MapPermission(Permission permission)
        {
            return new PermissionDto
            {
                Id = permission.Id,
                Name = permission.Name,
                DisplayName = permission.DisplayName,
                Group = permission.Group
            };
        }
    }
}
