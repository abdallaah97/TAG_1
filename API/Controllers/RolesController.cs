using Application.Common.Authorization;
using Application.Services.RoleService;
using Application.Services.RoleService.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HasPermission(Permissions.Roles.View)]
        [HttpGet("GetAllRoles")]
        public async Task<IActionResult> GetAllRoles([FromQuery] GetAllRolesInputDto input)
        {
            var roles = await _roleService.GetAllRoles(input);
            return Ok(roles);
        }

        [HasPermission(Permissions.Roles.View)]
        [HttpGet("GetRoleById")]
        public async Task<IActionResult> GetRoleById([FromQuery] int id)
        {
            var role = await _roleService.GetRoleById(id);
            return Ok(role);
        }

        [HasPermission(Permissions.Roles.Create)]
        [HttpPost("CreateRole")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto input)
        {
            var id = await _roleService.CreateRole(input);
            return Ok(new { id });
        }

        [HasPermission(Permissions.Roles.Update)]
        [HttpPut("UpdateRole")]
        public async Task<IActionResult> UpdateRole([FromBody] UpdateRoleDto input)
        {
            await _roleService.UpdateRole(input);
            return Ok();
        }

        [HasPermission(Permissions.Roles.Delete)]
        [HttpDelete("DeleteRole")]
        public async Task<IActionResult> DeleteRole([FromQuery] int id)
        {
            await _roleService.DeleteRole(id);
            return Ok();
        }

        // The full catalog of everything that can be granted, grouped per module.
        [HasPermission(Permissions.Roles.View)]
        [HttpGet("GetAllPermissions")]
        public async Task<IActionResult> GetAllPermissions()
        {
            var permissions = await _roleService.GetAllPermissions();
            return Ok(permissions);
        }

        [HasPermission(Permissions.Roles.View)]
        [HttpGet("GetRolePermissions")]
        public async Task<IActionResult> GetRolePermissions([FromQuery] int roleId)
        {
            var permissions = await _roleService.GetRolePermissions(roleId);
            return Ok(permissions);
        }

        // Replaces the whole permission set of the role with the one that is sent in.
        [HasPermission(Permissions.Roles.ManagePermissions)]
        [HttpPut("UpdateRolePermissions")]
        public async Task<IActionResult> UpdateRolePermissions([FromBody] UpdateRolePermissionsDto input)
        {
            await _roleService.UpdateRolePermissions(input);
            return Ok();
        }

        [HasPermission(Permissions.Roles.ManagePermissions)]
        [HttpPost("AddPermissionToRole")]
        public async Task<IActionResult> AddPermissionToRole([FromQuery] int roleId, [FromQuery] string permission)
        {
            await _roleService.AddPermissionToRole(roleId, permission);
            return Ok();
        }

        [HasPermission(Permissions.Roles.ManagePermissions)]
        [HttpDelete("RemovePermissionFromRole")]
        public async Task<IActionResult> RemovePermissionFromRole([FromQuery] int roleId, [FromQuery] string permission)
        {
            await _roleService.RemovePermissionFromRole(roleId, permission);
            return Ok();
        }

        [HasPermission(Permissions.Roles.View)]
        [HttpGet("GetRoleUsers")]
        public async Task<IActionResult> GetRoleUsers([FromQuery] int roleId)
        {
            var users = await _roleService.GetRoleUsers(roleId);
            return Ok(users);
        }
    }
}
