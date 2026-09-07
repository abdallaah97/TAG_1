using System.ComponentModel.DataAnnotations;

namespace Application.Services.AuthService.DTOs
{
    public class RefreshTokenInputDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
