using Application.Services.AuthService.DTOs;

namespace Application.Services.AuthService
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginInputDto input);
        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenInputDto input);
        Task LogoutAsync(RefreshTokenInputDto input);
        Task LogoutAllAsync();
        Task ChangePasswordAsync(ChangePasswordInputDto input);
        Task<CurrentUserDto> GetCurrentUserAsync();
        Task UpdateProfileAsync(UpdateProfileInputDto input);
    }
}
