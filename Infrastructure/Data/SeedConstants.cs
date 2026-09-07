namespace Infrastructure.Data
{
    public static class SeedConstants
    {
        // A fixed timestamp keeps the generated migrations deterministic.
        public static readonly DateTime SeedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public const int SuperAdminRoleId = 1;
        public const int AdminRoleId = 2;
        public const int UserRoleId = 3;
    }
}
