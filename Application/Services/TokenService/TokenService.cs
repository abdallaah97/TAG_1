using Application.Common.Authorization;
using Application.Common.Settings;
using Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Application.Services.TokenService
{
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;

        static TokenService()
        {
            // Keep the claim types exactly as they are written below instead of the long WS-* urls.
            JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();
        }

        public TokenService(IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }

        public AccessToken CreateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions)
        {
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("id", user.Id.ToString()),
                new Claim("name", user.Name),
                new Claim("email", user.Email),
                new Claim("phoneNumber", user.PhoneNumber ?? string.Empty)
            };



            claims.AddRange(roles.Distinct().Select(role => new Claim("role", role)));
            claims.AddRange(permissions.Distinct().Select(permission => new Claim(Permissions.ClaimType, permission)));

            var token = new JwtSecurityToken
            (
                claims: claims,
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                notBefore: DateTime.UtcNow,
                expires: expiresAt,
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key)),
                    SecurityAlgorithms.HmacSha256)
            );

            return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }

        public GeneratedRefreshToken CreateRefreshToken(int userId, string? ipAddress)
        {
            var random = new byte[64];
            RandomNumberGenerator.Fill(random);
            var token = Convert.ToBase64String(random);

            var entity = new RefreshToken
            {
                UserId = userId,
                TokenHash = Hash(token),
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                CreatedByIp = ipAddress,
                CreatedAt = DateTime.UtcNow
            };

            return new GeneratedRefreshToken(token, entity);
        }

        public string Hash(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }
    }
}
