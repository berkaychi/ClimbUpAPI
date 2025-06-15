using System.ComponentModel.DataAnnotations;

namespace ClimbUpAPI.Models.DTOs.UsersDTOs
{
    public class UserDTO
    {
        [Required]
        public string FullName { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class UserGetDTO
    {
        [Required]
        public string FullName { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public DateTime DateAdded { get; set; }
        public string? ProfilePictureUrl { get; set; }

        // Gamification Points
        public long TotalSteps { get; set; }
        public long Stepstones { get; set; }
    }

    public class LoginDTO
    {
        [Required]
        public string Email { get; set; } = null!;
        [Required]
        public string Password { get; set; } = null!;
    }

    public class UpdateProfileDTO
    {
        public string FullName { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? ProfilePictureUrl { get; set; }
    }

    public class ChangePasswordDTO
    {
        [Required]
        public string CurrentPassword { get; set; } = null!;
        [Required]
        public string NewPassword { get; set; } = null!;
    }

    public class ResetPasswordDTO
    {
        public string UserId { get; set; } = null!;
        public string Token { get; set; } = null!;
        public string Password { get; set; } = null!;
    }


    public class ForgotPasswordDTO
    {
        public string Email { get; set; } = null!;
    }

    public class LoginResponseDTO
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? ProfilePictureUrl { get; set; }
        public IList<string> Roles { get; set; } = [];
    }

    public class RefreshTokenRequestDTO
    {
        [Required]
        public string RefreshToken { get; set; } = null!;
    }
}
