namespace Application.Services.CacheService
{
    public static class CacheKeys
    {
        public const string Roles = "roles";

        public static readonly TimeSpan RolesExpiration = TimeSpan.FromHours(24);
    }
}
