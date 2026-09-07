using System.ComponentModel.DataAnnotations;

namespace Application.Services.UserService.DTOs
{
    public class GetUserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public List<UserRoleDto> Roles { get; set; } = new();
    }

    public class UserRoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class UserDetailsDto : GetUserDto
    {
        public List<string> Permissions { get; set; } = new();
    }

    public class CreateUserDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public List<int> RoleIds { get; set; } = new();
    }

    public class UpdateUserDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class ChangeUserPasswordInputDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class AssignRolesInputDto
    {
        [Required]
        public int UserId { get; set; }

        // The final set of roles of the user, everything that is not listed is removed.
        public List<int> RoleIds { get; set; } = new();
    }

    public class GetAllUsersInputDto
    {
        public string? Search { get; set; }
        public int? RoleId { get; set; }
        public bool? IsActive { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
