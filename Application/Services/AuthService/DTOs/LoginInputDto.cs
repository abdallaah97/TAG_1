using System.ComponentModel.DataAnnotations;

namespace Application.Services.AuthService.DTOs
{
    public class LoginInputDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
