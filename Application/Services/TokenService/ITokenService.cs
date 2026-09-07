using Domain.Entities;

namespace Application.Services.TokenService
{
    public interface ITokenService
    {
        AccessToken CreateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions);

        // Returns the clear token that is handed to the client together with the hash that is stored.
        GeneratedRefreshToken CreateRefreshToken(int userId, string? ipAddress);

        string Hash(string token);
    }

    public sealed record AccessToken(string Token, DateTime ExpiresAt);

    public sealed record GeneratedRefreshToken(string Token, RefreshToken Entity);
}
