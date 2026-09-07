using Application.Common.Authorization;
using Application.Services.UserService;
using Application.Services.UserService.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HasPermission(Permissions.Users.View)]
        [HttpGet("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers([FromQuery] GetAllUsersInputDto input)
        {
            var users = await _userService.GetAllUsers(input);
            return Ok(users);
        }

        [HasPermission(Permissions.Users.View)]
        [HttpGet("GetUserById")]
        public async Task<IActionResult> GetUserById([FromQuery] int id)
        {
            var user = await _userService.GetUserById(id);
            return Ok(user);
        }

        [HasPermission(Permissions.Users.Create)]
        [HttpPost("CreateUser")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto input)
        {
            var id = await _userService.CreateUser(input);
            return Ok(new { id });
        }

        [HasPermission(Permissions.Users.Update)]
        [HttpPut("UpdateUser")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserDto input)
        {
            await _userService.UpdateUser(input);
            return Ok();
        }

        [HasPermission(Permissions.Users.Delete)]
        [HttpDelete("DeleteUser")]
        public async Task<IActionResult> DeleteUser([FromQuery] int id)
        {
            await _userService.DeleteUser(id);
            return Ok();
        }

        [HasPermission(Permissions.Users.Update)]
        [HttpPut("SetUserActiveState")]
        public async Task<IActionResult> SetUserActiveState([FromQuery] int id, [FromQuery] bool isActive)
        {
            await _userService.SetUserActiveState(id, isActive);
            return Ok();
        }

        [HasPermission(Permissions.Users.ChangePassword)]
        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangeUserPasswordInputDto input)
        {
            await _userService.ChangePasswordAsync(input);
            return Ok();
        }

        // Replaces the whole role set of the user with the one that is sent in.
        [HasPermission(Permissions.Users.AssignRoles)]
        [HttpPut("AssignRoles")]
        public async Task<IActionResult> AssignRoles([FromBody] AssignRolesInputDto input)
        {
            await _userService.AssignRolesToUser(input);
            return Ok();
        }

        [HasPermission(Permissions.Users.AssignRoles)]
        [HttpPost("AddRoleToUser")]
        public async Task<IActionResult> AddRoleToUser([FromQuery] int userId, [FromQuery] int roleId)
        {
            await _userService.AddRoleToUser(userId, roleId);
            return Ok();
        }

        [HasPermission(Permissions.Users.AssignRoles)]
        [HttpDelete("RemoveRoleFromUser")]
        public async Task<IActionResult> RemoveRoleFromUser([FromQuery] int userId, [FromQuery] int roleId)
        {
            await _userService.RemoveRoleFromUser(userId, roleId);
            return Ok();
        }
    }
}
