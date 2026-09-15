using Application.Services.AuthService;
using Application.Services.AuthService.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginInputDto input)
        {
            var response = await _authService.LoginAsync(input);
            return Ok(response);
        }

        // Rotates the refresh token: the token that was sent in is revoked and a brand new
        // pair is handed back, built from the roles and permissions the user holds right now.
        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenInputDto input)
        {
            var response = await _authService.RefreshTokenAsync(input);
            return Ok(response);
        }

        [HttpPost("Logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenInputDto input)
        {
            await _authService.LogoutAsync(input);
            return Ok();
        }

        [Authorize]
        [HttpPost("LogoutAll")]
        public async Task<IActionResult> LogoutAll()
        {
            await _authService.LogoutAllAsync();
            return Ok();
        }

        [Authorize]
        [HttpGet("Me")]
        public async Task<IActionResult> Me()
        {
            var user = await _authService.GetCurrentUserAsync();
            return Ok(user);
        }

        [Authorize]
        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordInputDto input)
        {
            await _authService.ChangePasswordAsync(input);
            return Ok();
        }

        [Authorize]
        [HttpPut("UpdateProfile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileInputDto input)
        {
            await _authService.UpdateProfileAsync(input);
            return Ok();
        }
    }
}
