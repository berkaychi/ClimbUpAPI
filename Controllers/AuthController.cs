using ClimbUpAPI.Models.DTOs.UsersDTOs;
using ClimbUpAPI.Models;
using ClimbUpAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace ClimbUpAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly UserManager<AppUser> _userManager;

        public AuthController(IAuthService authService, ILogger<AuthController> logger, UserManager<AppUser> userManager)
        {
            _authService = authService;
            _logger = logger;
            _userManager = userManager;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserDTO dto)
        {
            _logger.LogDebug("Attempting to register user {UserEmail}", dto.Email);
            var result = await _authService.RegisterAsync(dto);
            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserEmail} registered successfully. EventType: {EventType}, LogCategory: {LogCategory}", dto.Email, "UserRegistrationSuccess", "SecurityAudit");
                return CreatedAtAction(nameof(UsersController.GetMyProfile), "Users", routeValues: null, new { message = "User registered successfully. Please check your email to confirm your account." });
            }
            else
            {
                var errors = FormatIdentityErrors(result);
                _logger.LogWarning("User registration failed for {UserEmail}. EventType: {EventType}, Reason: {@Errors}, LogCategory: {LogCategory}", dto.Email, "UserRegistrationFailure", errors, "SecurityAudit");
                return BadRequest(errors);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDTO dto)
        {
            _logger.LogDebug("Attempting to login user {LoginIdentifier}", dto.Email);
            try
            {
                var response = await _authService.LoginAsync(dto);
                _logger.LogInformation("Login attempt successful for {LoginIdentifier}. EventType: {EventType}, LogCategory: {LogCategory}", dto.Email, "LoginSuccess", "SecurityAudit");
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Login failed for {LoginIdentifier}. EventType: {EventType}, FailureReason: {FailureReason}, LogCategory: {LogCategory}", dto.Email, "LoginFailure", ex.Message, "SecurityAudit");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during login for {LoginIdentifier}", dto.Email);
                return StatusCode(500, new { message = "Giriş sırasında beklenmedik bir hata oluştu." });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDTO requestDto)
        {
            _logger.LogDebug("Attempting to refresh token.");
            if (requestDto == null || string.IsNullOrEmpty(requestDto.RefreshToken))
            {
                _logger.LogWarning("Refresh token request failed. Reason: Refresh token is required. EventType: {EventType}, LogCategory: {LogCategory}", "TokenRefreshFailure", "SecurityAudit");
                return BadRequest(new { message = "Refresh token gereklidir." });
            }

            try
            {
                var response = await _authService.RefreshTokenAsync(requestDto);
                _logger.LogInformation("Token refreshed successfully. EventType: {EventType}, LogCategory: {LogCategory}", "TokenRefreshSuccess", "SecurityAudit");
                return Ok(response);
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning(ex, "Token refresh failed due to security token issue. EventType: {EventType}, FailureReason: {FailureReason}, LogCategory: {LogCategory}", "TokenRefreshFailure", ex.Message, "SecurityAudit");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during token refresh. EventType: {EventType}, LogCategory: {LogCategory}", "TokenRefreshError", "SecurityAudit");
                return StatusCode(500, new { message = "Token yenilenirken beklenmedik bir hata oluştu." });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDTO requestDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogDebug("User {UserId} attempting to logout.", userId ?? "Unknown");

            if (requestDto == null || string.IsNullOrEmpty(requestDto.RefreshToken))
            {
                _logger.LogWarning("Logout failed for User {UserId}. Reason: Refresh token is required. EventType: {EventType}, LogCategory: {LogCategory}", userId ?? "Unknown", "LogoutFailure", "SecurityAudit");
                return BadRequest(new { message = "Oturumu sonlandırmak için refresh token gereklidir." });
            }
            try
            {
                await _authService.RevokeTokenAsync(requestDto.RefreshToken);
                _logger.LogInformation("User {UserId} logged out successfully. EventType: {EventType}, LogCategory: {LogCategory}", userId, "LogoutSuccess", "SecurityAudit");
                return Ok(new { message = "Başarıyla çıkış yapıldı." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during logout for User {UserId}. EventType: {EventType}, LogCategory: {LogCategory}", userId ?? "Unknown", "LogoutError", "SecurityAudit");
                return StatusCode(500, new { message = "Çıkış sırasında beklenmedik bir hata oluştu." });
            }
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
        {
            _logger.LogDebug("Attempting to confirm email for User {UserId}. Token provided: {TokenProvided}", userId, !string.IsNullOrEmpty(token));
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Email confirmation failed. Reason: UserId or token is missing. UserIdProvided: {UserIdProvided}, TokenProvided: {TokenProvided}, EventType: {EventType}, LogCategory: {LogCategory}", !string.IsNullOrEmpty(userId), !string.IsNullOrEmpty(token), "EmailConfirmationFailure", "SecurityAudit");
                return BadRequest(new { message = "User ID and token are required." });
            }

            var result = await _authService.ConfirmEmailAsync(userId, token);

            if (result.Succeeded)
            {
                _logger.LogInformation("Email confirmed successfully for User {UserId}. EventType: {EventType}, LogCategory: {LogCategory}", userId, "EmailConfirmationSuccess", "SecurityAudit");
                var user = await _userManager.FindByIdAsync(userId);
                return Ok(new { message = "E-posta başarıyla onaylandı.", userName = user?.UserName });
            }
            else
            {
                var errors = FormatIdentityErrors(result);
                _logger.LogWarning("Email confirmation failed for User {UserId}. EventType: {EventType}, Reason: {@Errors}, LogCategory: {LogCategory}", userId, "EmailConfirmationFailure", errors, "SecurityAudit");
                return BadRequest(FormatIdentityErrors(result, "E-posta onayı başarısız oldu."));
            }
        }

        [HttpPost("password/reset")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO dto)
        {
            _logger.LogDebug("Attempting to send password reset for Email {EmailIdentifier}", dto.Email);
            await _authService.SendPasswordResetAsync(dto);
            _logger.LogInformation("Password reset initiated for Email {EmailIdentifier}. EventType: {EventType}, LogCategory: {LogCategory}", dto.Email, "PasswordResetRequested", "SecurityAudit");
            return Ok(new { message = "If an account with that email exists, a password reset link has been sent." });
        }

        [HttpPost("password/reset/confirm")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO dto)
        {
            _logger.LogDebug("Attempting to reset password for User {UserId}", dto.UserId);
            var result = await _authService.ResetPasswordAsync(dto);
            if (result.Succeeded)
            {
                _logger.LogInformation("Password reset successfully for User {UserId}. EventType: {EventType}, LogCategory: {LogCategory}", dto.UserId, "PasswordResetSuccess", "SecurityAudit");
                return Ok(new { message = "Şifre başarıyla sıfırlandı." });
            }
            else
            {
                var errors = FormatIdentityErrors(result);
                _logger.LogWarning("Password reset failed for User {UserId}. EventType: {EventType}, Reason: {@Errors}, LogCategory: {LogCategory}", dto.UserId, "PasswordResetFailure", errors, "SecurityAudit");
                return BadRequest(FormatIdentityErrors(result, "Şifre sıfırlama başarısız oldu."));
            }
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                _logger.LogWarning("Unauthorized attempt to change password. No UserId found in claims. EventType: {EventType}, LogCategory: {LogCategory}", "PasswordChangeFailure", "SecurityAudit");
                return Unauthorized();
            }
            _logger.LogDebug("User {UserId} attempting to change password.", userId);

            var result = await _authService.ChangePasswordAsync(dto, userId);
            if (result.Succeeded)
            {
                _logger.LogInformation("Password changed successfully for User {UserId}. EventType: {EventType}, LogCategory: {LogCategory}", userId, "PasswordChangeSuccess", "SecurityAudit");
                return Ok(new { message = "Şifre başarıyla değiştirildi." });
            }
            else
            {
                var errors = FormatIdentityErrors(result);
                _logger.LogWarning("Password change failed for User {UserId}. EventType: {EventType}, Reason: {@Errors}, LogCategory: {LogCategory}", userId, "PasswordChangeFailure", errors, "SecurityAudit");
                return BadRequest(FormatIdentityErrors(result, "Şifre değiştirme başarısız oldu."));
            }
        }


        private static object FormatIdentityErrors(IdentityResult result, string defaultMessage = "İşlem başarısız oldu.")
        {
            var errorMessages = result.Errors
                .Select(e => e.Code switch
                {
                    "DuplicateUserName" => $"Kullanıcı adı '{e.Description.Split('\'')[1]}' zaten kullanılıyor.",
                    "DuplicateEmail" => $"E-posta adresi '{e.Description.Split('\'')[1]}' zaten kullanılıyor.",
                    "PasswordTooShort" => $"Şifre en az {e.Description.Split(' ')[2]} karakter uzunluğunda olmalıdır.",
                    "PasswordRequiresNonAlphanumeric" => "Şifre en az bir özel karakter (!@#$%^&*) içermelidir.",
                    "PasswordRequiresLower" => "Şifre en az bir küçük harf içermelidir.",
                    "PasswordRequiresUpper" => "Şifre en az bir büyük harf içermelidir.",
                    "PasswordRequiresDigit" => "Şifre en az bir rakam içermelidir.",
                    "InvalidToken" => "Geçersiz veya süresi dolmuş kod/bağlantı.",
                    "UserNotFound" => "Kullanıcı bulunamadı.",
                    "PasswordMismatch" => "Mevcut şifre yanlış.",
                    "DefaultError" => defaultMessage,
                    _ => e.Description
                })
                .ToList();


            if (errorMessages.Count == 1)
            {
                return new { message = errorMessages.First() };
            }
            else if (errorMessages.Count != 0)
            {
                return new { message = defaultMessage, errors = errorMessages };
            }
            else
            {
                return new { message = defaultMessage };
            }
        }
    }
}