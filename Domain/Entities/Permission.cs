namespace Domain.Entities
{
    // A single atomic right in the system, e.g. "Users.Create".
    // The catalog is seeded by the application and is never edited at runtime.
    public class Permission : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
