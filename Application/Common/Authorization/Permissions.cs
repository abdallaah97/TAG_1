namespace Application.Common.Authorization
{
    public sealed record PermissionDefinition(int Id, string Name, string DisplayName, string Group);

    // The single source of truth for every right in the system.
    // Ids are fixed so the catalog can be seeded deterministically through a migration.
    public static class Permissions
    {
        public const string ClaimType = "permission";

        public static class Users
        {
            public const string View = "Users.View";
            public const string Create = "Users.Create";
            public const string Update = "Users.Update";
            public const string Delete = "Users.Delete";
            public const string ChangePassword = "Users.ChangePassword";
            public const string AssignRoles = "Users.AssignRoles";
        }

        public static class Roles
        {
            public const string View = "Roles.View";
            public const string Create = "Roles.Create";
            public const string Update = "Roles.Update";
            public const string Delete = "Roles.Delete";
            public const string ManagePermissions = "Roles.ManagePermissions";
        }

        public static List<PermissionDefinition> All { get; } = new List<PermissionDefinition>
        {
            new(1, Users.View, "View users", "Users"),
            new(2, Users.Create, "Create users", "Users"),
            new(3, Users.Update, "Update users", "Users"),
            new(4, Users.Delete, "Delete users", "Users"),
            new(5, Users.ChangePassword, "Change the password of a user", "Users"),
            new(6, Users.AssignRoles, "Assign roles to a user", "Users"),

            new(7, Roles.View, "View roles and permissions", "Roles"),
            new(8, Roles.Create, "Create roles", "Roles"),
            new(9, Roles.Update, "Update roles", "Roles"),
            new(10, Roles.Delete, "Delete roles", "Roles"),
            new(11, Roles.ManagePermissions, "Grant or revoke the permissions of a role", "Roles")
        };

        public static bool Exists(string name) => All.Any(p => p.Name == name);
    }
}
