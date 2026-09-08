using System.ComponentModel.DataAnnotations;

namespace Application.Services.AuthService.DTOs
{
    public class ChangePasswordInputDto
    {
        [Required]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
