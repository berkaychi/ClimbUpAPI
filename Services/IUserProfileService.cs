using ClimbUpAPI.Models.DTOs.UsersDTOs;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace ClimbUpAPI.Services
{
    public interface IUserProfileService
    {
        Task<UserGetDTO?> GetUserByIdAsync(string id);
        Task<UserGetDTO?> GetUserByUserIdAsync(string userId);
        Task<IdentityResult> UpdateProfileAsync(UpdateProfileDTO dto, string userId);
        Task<bool> UpdateProfilePictureAsync(string userId, string profilePictureUrl);
    }
}