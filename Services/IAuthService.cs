using ClimbUpAPI.Models.DTOs.UsersDTOs;
using Microsoft.AspNetCore.Identity;

namespace ClimbUpAPI.Services
{
    public interface IAuthService
    {
        Task<IdentityResult> RegisterAsync(UserDTO dto);
        Task<LoginResponseDTO> LoginAsync(LoginDTO dto);
        Task<IdentityResult> ConfirmEmailAsync(string userId, string token);
        Task SendPasswordResetAsync(ForgotPasswordDTO dto);
        Task<IdentityResult> ResetPasswordAsync(ResetPasswordDTO dto);
        Task<IdentityResult> ChangePasswordAsync(ChangePasswordDTO dto, string userId);
        Task<LoginResponseDTO> RefreshTokenAsync(RefreshTokenRequestDTO requestDto);
        Task RevokeTokenAsync(string token);
    }
}