using Application.Services.RoleService.DTOs;

namespace Application.Services.RoleService
{
    public interface IRoleService
    {
        Task<List<GetRoleDto>> GetAllRoles(GetAllRolesInputDto input);
        Task<RoleDetailsDto> GetRoleById(int id);
        Task<int> CreateRole(CreateRoleDto input);
        Task UpdateRole(UpdateRoleDto input);
        Task DeleteRole(int id);

        Task<List<PermissionGroupDto>> GetAllPermissions();
        Task<List<PermissionDto>> GetRolePermissions(int roleId);
        Task UpdateRolePermissions(UpdateRolePermissionsDto input);
        Task AddPermissionToRole(int roleId, string permission);
        Task RemovePermissionFromRole(int roleId, string permission);

        Task<List<RoleUserDto>> GetRoleUsers(int roleId);
    }
}
