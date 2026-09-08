using Application.Repositories;
using Application.Services.CurrentUserService;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.SecurityService
{
    public class UserSecurityService : IUserSecurityService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public UserSecurityService(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public Task RevokeUserTokensAsync(int userId, string reason)
            => RevokeUsersTokensAsync(new[] { userId }, reason);

        public async Task RevokeUsersTokensAsync(IEnumerable<int> userIds, string reason)
        {
            var ids = userIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return;
            }

            var tokens = await _unitOfWork.RefreshTokens.GetAll()
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

            _unitOfWork.RefreshTokens.UpdateRange(tokens);
        }
    }
}
