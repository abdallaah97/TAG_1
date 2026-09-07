using System.ComponentModel.DataAnnotations;

namespace Application.Services.AuthService.DTOs
{
    public class UpdateProfileInputDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;
    }
}
