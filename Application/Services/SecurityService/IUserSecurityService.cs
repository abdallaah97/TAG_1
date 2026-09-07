namespace Application.Services.SecurityService
{
    // Whenever what a user is allowed to do changes, their sessions have to be cut.
    // The access token stays valid until it expires, the refresh token does not survive the change.
    public interface IUserSecurityService
    {
        Task RevokeUserTokensAsync(int userId, string reason, bool saveChanges = true);
        Task RevokeUsersTokensAsync(IEnumerable<int> userIds, string reason, bool saveChanges = true);
    }
}
