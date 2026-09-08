using Application.Common.Models;
using Application.Services.UserService.DTOs;
using Application.Services.UserService.Export;

namespace Application.Services.UserService
{
    public interface IUserService
    {
        Task<PagedResult<GetUserDto>> GetAllUsers(GetAllUsersInputDto input);
        Task<ExportFile> ExportUsers(GetAllUsersInputDto input, string format);
        Task<UserDetailsDto> GetUserById(int id);
        Task<int> CreateUser(CreateUserDto input);
        Task UpdateUser(UpdateUserDto input);
        Task DeleteUser(int id);
        Task SetUserActiveState(int id, bool isActive);
        Task ChangePasswordAsync(ChangeUserPasswordInputDto input);
        Task AssignRolesToUser(AssignRolesInputDto input);
        Task AddRoleToUser(int userId, int roleId);
        Task RemoveRoleFromUser(int userId, int roleId);
    }
}
