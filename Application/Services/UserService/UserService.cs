using Application.Common.Exceptions;
using Application.Common.Models;
using Application.Repositories;
using Application.Services.CurrentUserService;
using Application.Services.SecurityService;
using Application.Services.UserService.DTOs;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.UserService
{
    public class UserService : IUserService
    {
        private readonly IGenericRepository<User> _userRepository;
        private readonly IGenericRepository<Role> _roleRepository;
        private readonly IGenericRepository<UserRole> _userRoleRepository;
        private readonly IUserSecurityService _userSecurityService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UserService(
            IGenericRepository<User> userRepository,
            IGenericRepository<Role> roleRepository,
            IGenericRepository<UserRole> userRoleRepository,
            IUserSecurityService userSecurityService,
            ICurrentUserService currentUserService,
            IPasswordHasher<User> passwordHasher)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _userRoleRepository = userRoleRepository;
            _userSecurityService = userSecurityService;
            _currentUserService = currentUserService;
            _passwordHasher = passwordHasher;
        }

        public async Task<PagedResult<GetUserDto>> GetAllUsers(GetAllUsersInputDto input)
        {
            var pageNumber = input.PageNumber < 1 ? 1 : input.PageNumber;
            var pageSize = input.PageSize is < 1 or > 200 ? 20 : input.PageSize;

            var query = _userRepository.GetAllReadOnly();

            if (!string.IsNullOrWhiteSpace(input.Search))
            {
                var search = input.Search.Trim();
                query = query.Where(u => u.Name.Contains(search) || u.Email.Contains(search) || u.PhoneNumber.Contains(search));
            }

            if (input.RoleId.HasValue)
            {
                query = query.Where(u => u.UserRoles.Any(ur => ur.RoleId == input.RoleId.Value));
            }

            if (input.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == input.IsActive.Value);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(u => u.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new GetUserDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt,
                    Roles = u.UserRoles.Select(ur => new UserRoleDto { Id = ur.RoleId, Name = ur.Role.Name }).ToList()
                })
                .ToListAsync();

            return new PagedResult<GetUserDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<UserDetailsDto> GetUserById(int id)
        {
            var user = await _userRepository.GetAllReadOnly()
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Id == id)
                ?? throw new NotFoundException("User", id);

            return new UserDetailsDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                Roles = user.UserRoles.Select(ur => new UserRoleDto { Id = ur.RoleId, Name = ur.Role.Name }).ToList(),
                Permissions = user.UserRoles
                    .SelectMany(ur => ur.Role.RolePermissions)
                    .Select(rp => rp.Permission.Name)
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList()
            };
        }

        public async Task<int> CreateUser(CreateUserDto input)
        {
            var email = input.Email.Trim();

            var isEmailTaken = await _userRepository.GetAllReadOnly()
                .AnyAsync(u => u.Email.ToLower() == email.ToLower());

            if (isEmailTaken)
            {
                throw new ConflictException("Email already exists");
            }

            var roles = await LoadRolesAsync(input.RoleIds);

            var user = new User
            {
                Name = input.Name.Trim(),
                Email = email,
                PhoneNumber = input.PhoneNumber?.Trim() ?? string.Empty,
                IsActive = input.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            user.Password = _passwordHasher.HashPassword(user, input.Password);

            user.UserRoles = roles
                .Select(role => new UserRole { RoleId = role.Id, AssignedAt = DateTime.UtcNow })
                .ToList();

            await _userRepository.InsertAsync(user);
            await _userRepository.SaveChangesAsync();

            return user.Id;
        }

        public async Task UpdateUser(UpdateUserDto input)
        {
            var user = await _userRepository.GetByIdAsync(input.Id)
                ?? throw new NotFoundException("User", input.Id);

            var email = input.Email.Trim();

            var isEmailTaken = await _userRepository.GetAllReadOnly()
                .AnyAsync(u => u.Email.ToLower() == email.ToLower() && u.Id != input.Id);

            if (isEmailTaken)
            {
                throw new ConflictException("Email already exists");
            }

            user.Name = input.Name.Trim();
            user.Email = email;
            user.PhoneNumber = input.PhoneNumber?.Trim() ?? string.Empty;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task DeleteUser(int id)
        {
            var user = await _userRepository.GetAll()
                .Include(u => u.UserRoles)
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Id == id)
                ?? throw new NotFoundException("User", id);

            if (id == _currentUserService.UserId)
            {
                throw new BadRequestException("You can not delete your own account");
            }

            await GuardLastSuperAdminAsync(user, "The last super admin can not be deleted");

            _userRoleRepository.DeleteRange(user.UserRoles);
            _userRepository.Delete(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task SetUserActiveState(int id, bool isActive)
        {
            var user = await _userRepository.GetAll()
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id)
                ?? throw new NotFoundException("User", id);

            if (!isActive)
            {
                if (id == _currentUserService.UserId)
                {
                    throw new BadRequestException("You can not disable your own account");
                }

                await GuardLastSuperAdminAsync(user, "The last super admin can not be disabled");
            }

            user.IsActive = isActive;
            _userRepository.Update(user);

            if (!isActive)
            {
                await _userSecurityService.RevokeUserTokensAsync(user.Id, RevokeReasons.UserDeactivated, saveChanges: false);
            }

            await _userRepository.SaveChangesAsync();
        }

        public async Task ChangePasswordAsync(ChangeUserPasswordInputDto input)
        {
            if (input.NewPassword != input.ConfirmNewPassword)
            {
                throw new BadRequestException("New password and confirm new password do not match");
            }

            var user = await _userRepository.GetByIdAsync(input.UserId)
                ?? throw new NotFoundException("User", input.UserId);

            user.Password = _passwordHasher.HashPassword(user, input.NewPassword);
            _userRepository.Update(user);

            await _userSecurityService.RevokeUserTokensAsync(user.Id, RevokeReasons.PasswordChanged, saveChanges: false);

            await _userRepository.SaveChangesAsync();
        }

        public async Task AssignRolesToUser(AssignRolesInputDto input)
        {
            var user = await _userRepository.GetAll()
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == input.UserId)
                ?? throw new NotFoundException("User", input.UserId);

            var roles = await LoadRolesAsync(input.RoleIds);
            var requestedIds = roles.Select(r => r.Id).ToHashSet();

            var losesSuperAdmin = user.UserRoles.Any(ur => ur.Role.Name == SystemRoles.SuperAdmin)
                                  && !roles.Any(r => r.Name == SystemRoles.SuperAdmin);

            if (losesSuperAdmin)
            {
                await GuardLastSuperAdminAsync(user, "The last super admin can not lose the super admin role");
            }

            var removed = user.UserRoles.Where(ur => !requestedIds.Contains(ur.RoleId)).ToList();
            var addedIds = requestedIds.Except(user.UserRoles.Select(ur => ur.RoleId)).ToList();

            if (removed.Count == 0 && addedIds.Count == 0)
            {
                return;
            }

            _userRoleRepository.DeleteRange(removed);

            await _userRoleRepository.InsertRangeAsync(addedIds
                .Select(roleId => new UserRole { UserId = user.Id, RoleId = roleId, AssignedAt = DateTime.UtcNow })
                .ToList());

            await _userSecurityService.RevokeUserTokensAsync(user.Id, RevokeReasons.SecurityChanged, saveChanges: false);

            await _userRoleRepository.SaveChangesAsync();
        }

        public async Task AddRoleToUser(int userId, int roleId)
        {
            var user = await _userRepository.GetAllReadOnly().FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new NotFoundException("User", userId);

            _ = await _roleRepository.GetAllReadOnly().FirstOrDefaultAsync(r => r.Id == roleId)
                ?? throw new NotFoundException("Role", roleId);

            var alreadyAssigned = await _userRoleRepository.GetAllReadOnly()
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (alreadyAssigned)
            {
                throw new ConflictException("The role is already assigned to this user");
            }

            await _userRoleRepository.InsertAsync(new UserRole
            {
                UserId = user.Id,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow
            });

            await _userSecurityService.RevokeUserTokensAsync(user.Id, RevokeReasons.SecurityChanged, saveChanges: false);

            await _userRoleRepository.SaveChangesAsync();
        }

        public async Task RemoveRoleFromUser(int userId, int roleId)
        {
            var userRole = await _userRoleRepository.GetAll()
                .Include(ur => ur.Role)
                .Include(ur => ur.User)
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId)
                ?? throw new NotFoundException("The role is not assigned to this user");

            if (userRole.Role.Name == SystemRoles.SuperAdmin)
            {
                await GuardLastSuperAdminAsync(userRole.User, "The last super admin can not lose the super admin role");
            }

            _userRoleRepository.Delete(userRole);

            await _userSecurityService.RevokeUserTokensAsync(userId, RevokeReasons.SecurityChanged, saveChanges: false);

            await _userRoleRepository.SaveChangesAsync();
        }

        private async Task<List<Role>> LoadRolesAsync(List<int> roleIds)
        {
            var ids = roleIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new List<Role>();
            }

            var roles = await _roleRepository.GetAllReadOnly()
                .Where(r => ids.Contains(r.Id))
                .ToListAsync();

            var missing = ids.Except(roles.Select(r => r.Id)).ToList();
            if (missing.Count > 0)
            {
                throw new NotFoundException($"The following roles were not found: {string.Join(", ", missing)}");
            }

            return roles;
        }

        // The system must never end up without a single active super admin.
        private async Task GuardLastSuperAdminAsync(User user, string message)
        {
            var isSuperAdmin = await _userRoleRepository.GetAllReadOnly()
                .AnyAsync(ur => ur.UserId == user.Id && ur.Role.Name == SystemRoles.SuperAdmin);

            if (!isSuperAdmin)
            {
                return;
            }

            var otherSuperAdmins = await _userRoleRepository.GetAllReadOnly()
                .CountAsync(ur => ur.Role.Name == SystemRoles.SuperAdmin && ur.UserId != user.Id && ur.User.IsActive);

            if (otherSuperAdmins == 0)
            {
                throw new BadRequestException(message);
            }
        }
    }
}
