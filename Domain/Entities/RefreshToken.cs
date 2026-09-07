namespace Domain.Entities
{
    // Only the hash of the token is persisted, so a database leak can not be replayed.
    public class RefreshToken : BaseEntity
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public string TokenHash { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }

        public DateTime? RevokedAt { get; set; }
        public string? RevokedReason { get; set; }
        public string? ReplacedByTokenHash { get; set; }

        public string? CreatedByIp { get; set; }
        public string? RevokedByIp { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsRevoked => RevokedAt.HasValue;
        public bool IsActive => !IsRevoked && !IsExpired;
    }

    public static class RevokeReasons
    {
        public const string Rotated = "Replaced by a new token";
        public const string Logout = "Logged out";
        public const string LogoutAll = "Logged out from all devices";
        public const string ReuseDetected = "Reuse of a revoked token was detected";
        public const string PasswordChanged = "Password was changed";
        public const string SecurityChanged = "Roles or permissions were changed";
        public const string UserDeactivated = "User was deactivated";
    }
}
