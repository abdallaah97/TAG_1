using System.ComponentModel.DataAnnotations;

namespace Application.Services.RoleService.DTOs
{
    public class GetRoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSystemRole { get; set; }
        public DateTime CreatedAt { get; set; }
        public int UsersCount { get; set; }
        public int PermissionsCount { get; set; }
        public List<PermissionDto> Permissions { get; set; }
    }

    public class RoleDetailsDto : GetRoleDto
    {
        public List<PermissionDto> Permissions { get; set; } = new();
    }

    public class PermissionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
    }

    public class PermissionGroupDto
    {
        public string Group { get; set; } = string.Empty;
        public List<PermissionDto> Permissions { get; set; } = new();
    }

    public class CreateRoleDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        // Optional: the role can be created empty and get its permissions later.
        public List<string> Permissions { get; set; } = new();
    }

    public class UpdateRoleDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public class UpdateRolePermissionsDto
    {
        [Required]
        public int RoleId { get; set; }

        // The final set of permissions of the role, everything that is not listed is revoked.
        public List<string> Permissions { get; set; } = new();
    }

    public class GetAllRolesInputDto
    {
        public string? Search { get; set; }
    }

    public class RoleUserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
