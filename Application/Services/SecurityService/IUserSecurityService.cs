namespace Application.Services.SecurityService
{
    // Whenever what a user is allowed to do changes, their sessions have to be cut.
    // The access token stays valid until it expires, the refresh token does not survive the change.
    //
    // The revocation is only staged in the unit of work; the caller commits it together with
    // the change that caused it.
    public interface IUserSecurityService
    {
        Task RevokeUserTokensAsync(int userId, string reason);
        Task RevokeUsersTokensAsync(IEnumerable<int> userIds, string reason);
    }
}
