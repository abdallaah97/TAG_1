using Application.Repositories;
using Application.Services.CurrentUserService;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.SecurityService
{
    public class UserSecurityService : IUserSecurityService
    {
        private readonly IGenericRepository<RefreshToken> _refreshTokenRepository;
        private readonly ICurrentUserService _currentUserService;

        public UserSecurityService(
            IGenericRepository<RefreshToken> refreshTokenRepository,
            ICurrentUserService currentUserService)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _currentUserService = currentUserService;
        }

        public Task RevokeUserTokensAsync(int userId, string reason, bool saveChanges = true)
            => RevokeUsersTokensAsync(new[] { userId }, reason, saveChanges);

        public async Task RevokeUsersTokensAsync(IEnumerable<int> userIds, string reason, bool saveChanges = true)
        {
            var ids = userIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return;
            }

            var tokens = await _refreshTokenRepository.GetAll()
                .Where(t => ids.Contains(t.UserId) && t.RevokedAt == null)
                .ToListAsync();

            if (tokens.Count == 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var ip = _currentUserService.IpAddress;

            foreach (var token in tokens)
            {
                token.RevokedAt = now;
                token.RevokedReason = reason;
                token.RevokedByIp = ip;
            }

            _refreshTokenRepository.UpdateRange(tokens);

            if (saveChanges)
            {
                await _refreshTokenRepository.SaveChangesAsync();
            }
        }
    }
}
