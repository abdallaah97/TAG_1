using Application.Common.Exceptions;
using Application.Repositories;
using Application.Services.AuthService.DTOs;
using Application.Services.CurrentUserService;
using Application.Services.SecurityService;
using Application.Services.TokenService;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.AuthService
{
    public class AuthService : IAuthService
    {
        private readonly IGenericRepository<User> _userRepository;
        private readonly IGenericRepository<RefreshToken> _refreshTokenRepository;
        private readonly ITokenService _tokenService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserSecurityService _userSecurityService;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AuthService(
            IGenericRepository<User> userRepository,
            IGenericRepository<RefreshToken> refreshTokenRepository,
            ITokenService tokenService,
            ICurrentUserService currentUserService,
            IUserSecurityService userSecurityService,
            IPasswordHasher<User> passwordHasher)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _tokenService = tokenService;
            _currentUserService = currentUserService;
            _userSecurityService = userSecurityService;
            _passwordHasher = passwordHasher;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginInputDto input)
        {
            var username = input.Username.Trim().ToLower();

            var user = await WithRolesAndPermissions()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == username);

            // The same message is returned for an unknown user and for a wrong password, so the
            // endpoint can not be used to find out which e-mail addresses exist.
            if (user == null)
            {
                throw new UnauthorizedException("Invalid username or password");
            }

            var verification = _passwordHasher.VerifyHashedPassword(user, user.Password, input.Password);
            if (verification == PasswordVerificationResult.Failed)
            {
                throw new UnauthorizedException("Invalid username or password");
            }

            if (!user.IsActive)
            {
                throw new ForbiddenException("This account is disabled");
            }

            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.Password = _passwordHasher.HashPassword(user, input.Password);
            }

            user.LastLoginAt = DateTime.UtcNow;
            _userRepository.Update(user);

            await RemoveObsoleteTokensAsync(user.Id);

            return await IssueTokensAsync(user);
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenInputDto input)
        {
            var hash = _tokenService.Hash(input.RefreshToken);

            var storedToken = await _refreshTokenRepository.GetAll()
                .FirstOrDefaultAsync(t => t.TokenHash == hash);

            if (storedToken == null)
            {
                throw new UnauthorizedException("Invalid refresh token");
            }

            // A token that was already rotated away is being replayed: the whole family is
            // treated as compromised, so every session of that user is dropped.
            if (storedToken.IsRevoked)
            {
                await _userSecurityService.RevokeUserTokensAsync(storedToken.UserId, RevokeReasons.ReuseDetected);
                throw new UnauthorizedException("Invalid refresh token");
            }

            if (storedToken.IsExpired)
            {
                throw new UnauthorizedException("Refresh token has expired");
            }

            var user = await WithRolesAndPermissions()
                .FirstOrDefaultAsync(u => u.Id == storedToken.UserId);

            if (user == null)
            {
                throw new UnauthorizedException("Invalid refresh token");
            }

            if (!user.IsActive)
            {
                throw new ForbiddenException("This account is disabled");
            }

            // Roles and permissions are read again from the database, so a refresh always
            // hands back a token that reflects the current state of the account.
            var response = await IssueTokensAsync(user, storedToken, saveChanges: false);

            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.RevokedReason = RevokeReasons.Rotated;
            storedToken.RevokedByIp = _currentUserService.IpAddress;
            _refreshTokenRepository.Update(storedToken);

            await _refreshTokenRepository.SaveChangesAsync();

            return response;
        }

        public async Task LogoutAsync(RefreshTokenInputDto input)
        {
            var hash = _tokenService.Hash(input.RefreshToken);

            var storedToken = await _refreshTokenRepository.GetAll()
                .FirstOrDefaultAsync(t => t.TokenHash == hash && t.RevokedAt == null);

            if (storedToken == null)
            {
                return;
            }

            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.RevokedReason = RevokeReasons.Logout;
            storedToken.RevokedByIp = _currentUserService.IpAddress;

            _refreshTokenRepository.Update(storedToken);
            await _refreshTokenRepository.SaveChangesAsync();
        }

        public async Task LogoutAllAsync()
        {
            await _userSecurityService.RevokeUserTokensAsync(RequiredUserId(), RevokeReasons.LogoutAll);
        }

        public async Task ChangePasswordAsync(ChangePasswordInputDto input)
        {
            if (input.NewPassword != input.ConfirmNewPassword)
            {
                throw new BadRequestException("New password and confirm new password do not match");
            }

            var userId = RequiredUserId();

            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new NotFoundException("User", userId);

            var verification = _passwordHasher.VerifyHashedPassword(user, user.Password, input.OldPassword);
            if (verification == PasswordVerificationResult.Failed)
            {
                throw new BadRequestException("Old password is incorrect");
            }

            user.Password = _passwordHasher.HashPassword(user, input.NewPassword);
            _userRepository.Update(user);

            // Every other device has to sign in again with the new password.
            await _userSecurityService.RevokeUserTokensAsync(user.Id, RevokeReasons.PasswordChanged, saveChanges: false);

            await _userRepository.SaveChangesAsync();
        }

        public async Task<CurrentUserDto> GetCurrentUserAsync()
        {
            var userId = RequiredUserId();

            var user = await WithRolesAndPermissions().AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new NotFoundException("User", userId);

            return MapToCurrentUser(user);
        }

        public async Task UpdateProfileAsync(UpdateProfileInputDto input)
        {
            var userId = RequiredUserId();

            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new NotFoundException("User", userId);

            var email = input.Email.Trim();

            var isEmailTaken = await _userRepository.GetAllReadOnly()
                .AnyAsync(u => u.Email.ToLower() == email.ToLower() && u.Id != userId);

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

        private IQueryable<User> WithRolesAndPermissions()
        {
            return _userRepository.GetAll()
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission);
        }

        private async Task<AuthResponseDto> IssueTokensAsync(User user, RefreshToken? rotatedFrom = null, bool saveChanges = true)
        {
            var roles = GetRoles(user);
            var permissions = GetPermissions(user);

            var accessToken = _tokenService.CreateAccessToken(user, roles, permissions);
            var refreshToken = _tokenService.CreateRefreshToken(user.Id, _currentUserService.IpAddress);

            await _refreshTokenRepository.InsertAsync(refreshToken.Entity);

            if (rotatedFrom != null)
            {
                rotatedFrom.ReplacedByTokenHash = refreshToken.Entity.TokenHash;
            }

            if (saveChanges)
            {
                await _refreshTokenRepository.SaveChangesAsync();
            }

            return new AuthResponseDto
            {
                AccessToken = accessToken.Token,
                AccessTokenExpiration = accessToken.ExpiresAt,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiration = refreshToken.Entity.ExpiresAt,
                User = MapToCurrentUser(user, roles, permissions)
            };
        }

        // Housekeeping: tokens that expired or were revoked longer than the retention window ago are dropped.
        private async Task RemoveObsoleteTokensAsync(int userId)
        {
            var threshold = DateTime.UtcNow.AddDays(-30);

            var obsolete = await _refreshTokenRepository.GetAll()
                .Where(t => t.UserId == userId && (t.ExpiresAt < threshold || (t.RevokedAt != null && t.RevokedAt < threshold)))
                .ToListAsync();

            if (obsolete.Count > 0)
            {
                _refreshTokenRepository.DeleteRange(obsolete);
            }
        }

        private int RequiredUserId()
        {
            return _currentUserService.UserId ?? throw new UnauthorizedException("User is not authenticated");
        }

        private static List<string> GetRoles(User user)
        {
            return user.UserRoles.Select(ur => ur.Role.Name).Distinct().OrderBy(name => name).ToList();
        }

        private static List<string> GetPermissions(User user)
        {
            return user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .OrderBy(name => name)
                .ToList();
        }

        private static CurrentUserDto MapToCurrentUser(User user, List<string>? roles = null, List<string>? permissions = null)
        {
            return new CurrentUserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                Roles = roles ?? GetRoles(user),
                Permissions = permissions ?? GetPermissions(user)
            };
        }
    }
}
