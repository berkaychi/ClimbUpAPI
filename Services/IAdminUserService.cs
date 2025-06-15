using ClimbUpAPI.Models.DTOs.Admin;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClimbUpAPI.Services
{
    public interface IAdminUserService
    {
        Task<List<AdminUserListDto>> GetUsersForAdminAsync();
        Task<IdentityResult> AssignRoleAsync(string userId, string roleName);
        Task<IdentityResult> RemoveRoleAsync(string userId, string roleName);
        Task<IList<string>> GetUserRolesAsync(string userId);
        Task<IdentityResult> DeleteUserByAdminAsync(string userId);
    }
}