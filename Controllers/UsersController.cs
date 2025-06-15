using ClimbUpAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ClimbUpAPI.Models.DTOs.UsersDTOs;
using ClimbUpAPI.Services;
using Microsoft.AspNetCore.Http;

namespace ClimbUpAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserProfileService _userProfileService;
        private readonly IAccountManagementService _accountManagementService;
        private readonly IUserSessionService _userSessionService;
        private readonly IAdminUserService _adminUserService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<UsersController> _logger;
        private readonly IAuthService _authService;

        public UsersController(
            IUserProfileService userProfileService,
            IAccountManagementService accountManagementService,
            IUserSessionService userSessionService,
            IAdminUserService adminUserService,
            ICloudinaryService cloudinaryService,
            ILogger<UsersController> logger,
            IAuthService authService)
        {
            _userProfileService = userProfileService;
            _accountManagementService = accountManagementService;
            _userSessionService = userSessionService;
            _adminUserService = adminUserService;
            _cloudinaryService = cloudinaryService;
            _logger = logger;
            _authService = authService;
            _logger.LogInformation("UsersController initialized for user management.");
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                _logger.LogWarning("Unauthorized attempt to get own profile. No UserId found in claims.");
                return Unauthorized();
            }
            _logger.LogDebug("User {UserId} attempting to get their own profile.", userId);

            try
            {
                var userDto = await _userProfileService.GetUserByUserIdAsync(userId);
                if (userDto == null)
                {
                    _logger.LogWarning("User profile not found for User {UserId}.", userId);
                    return NotFound(new { message = "User profile not found." });
                }

                _logger.LogInformation("Successfully retrieved profile with points for User {UserId}. UserName: {UserName}, Email: {Email}, TotalSteps: {TotalSteps}, Stepstones: {Stepstones}",
                    userId, userDto.UserName, userDto.Email, userDto.TotalSteps, userDto.Stepstones);
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Full profile data with points for User {UserId}: {@UserGetDTO}", userId, userDto);
                }
                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} getting their own profile.", userId);
                return StatusCode(500, new { message = "Profil alınırken bir hata oluştu." });
            }
        }

        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogDebug("Admin User {AdminUserId} attempting to get all users.", adminUserId ?? "UnknownAdmin");
            try
            {
                var users = await _adminUserService.GetUsersForAdminAsync();
                _logger.LogInformation("Admin User {AdminUserId} successfully retrieved {UserCount} users.", adminUserId ?? "UnknownAdmin", users.Count());
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("All users retrieved by Admin {AdminUserId}: {@Users}", adminUserId ?? "UnknownAdmin", users);
                }
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while Admin User {AdminUserId} getting all users.", adminUserId ?? "UnknownAdmin");
                return StatusCode(500, new { message = "Tüm kullanıcılar alınırken bir hata oluştu." });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUser(string id)
        {
            var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogDebug("Admin User {AdminUserId} attempting to get user {TargetUserId}.", adminUserId ?? "UnknownAdmin", id);
            try
            {
                var user = await _userProfileService.GetUserByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("User {TargetUserId} not found by Admin {AdminUserId}.", id, adminUserId ?? "UnknownAdmin");
                    return NotFound();
                }
                _logger.LogInformation("Admin User {AdminUserId} successfully retrieved user {TargetUserId}. UserName: {UserName}, Email: {Email}", adminUserId ?? "UnknownAdmin", id, user.UserName, user.Email);
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Full user data for {TargetUserId} retrieved by Admin {AdminUserId}: {@UserGetDTO}", id, adminUserId ?? "UnknownAdmin", user);
                }
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while Admin User {AdminUserId} getting user {TargetUserId}.", adminUserId ?? "UnknownAdmin", id);
                return StatusCode(500, new { message = "Kullanıcı alınırken bir hata oluştu." });
            }
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDTO dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                _logger.LogWarning("Unauthorized attempt to update profile. No UserId found in claims.");
                return Unauthorized();
            }
            _logger.LogDebug("User {UserId} attempting to update their profile with DTO: {@UpdateProfileDTO}", userId, dto);

            try
            {
                var currentUserData = await _userProfileService.GetUserByUserIdAsync(userId);
                if (currentUserData == null)
                {
                    _logger.LogWarning("User {UserId} not found when attempting to update profile.", userId);
                    return NotFound(new { message = "Kullanıcı bulunamadı." });
                }
                var originalEmail = currentUserData.Email;

                var result = await _userProfileService.UpdateProfileAsync(dto, userId);

                if (result.Succeeded)
                {
                    bool emailChangeInitiated = !string.IsNullOrEmpty(dto.Email) &&
                                                !string.Equals(originalEmail, dto.Email, StringComparison.OrdinalIgnoreCase);

                    if (emailChangeInitiated)
                    {
                        _logger.LogInformation("User {UserId} profile updated and email change initiated to {NewEmail}. Confirmation required. EventType: {EventType}, LogCategory: {LogCategory}", userId, dto.Email, "UserProfileUpdateSuccessWithEmailConfirmation", "SecurityAudit");
                        return Ok(new { message = "Profil güncellendi. E-posta değişikliğinizin tamamlanması için lütfen yeni e-posta adresinize gönderilen onay bağlantısına tıklayın." });
                    }
                    else
                    {
                        _logger.LogInformation("User {UserId} successfully updated their profile (no email change). EventType: {EventType}, LogCategory: {LogCategory}", userId, "UserProfileUpdateSuccess", "SecurityAudit");
                        return Ok(new { message = "Profil başarıyla güncellendi." });
                    }
                }
                else
                {
                    var errors = FormatIdentityErrors(result);
                    _logger.LogWarning("Profile update failed for User {UserId}. EventType: {EventType}, Reason: {@Errors}, LogCategory: {LogCategory}", userId, "UserProfileUpdateFailure", errors, "SecurityAudit");
                    return BadRequest(errors);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} updating their profile. DTO: {@UpdateProfileDTO}", userId, dto);
                return StatusCode(500, new { message = "Profil güncellenirken bir hata oluştu." });
            }
        }

        [HttpGet("confirm-email-change")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmailChange([FromQuery] string userId, [FromQuery] string token)
        {
            _logger.LogInformation("Attempting to confirm email change for User {UserId} with Token {Token}. EventType: {EventType}, LogCategory: {LogCategory}", userId, token, "EmailChangeConfirmationAttempted", "SecurityAudit");
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("ConfirmEmailChange request failed due to missing userId or token. UserId: {UserId}, Token: {Token}. EventType: {EventType}, LogCategory: {LogCategory}", userId, token, "EmailChangeConfirmationInvalidParams", "SecurityAudit");
                return BadRequest(new { message = "Kullanıcı ID'si ve token gereklidir." });
            }

            try
            {
                var result = await _accountManagementService.ConfirmEmailChangeAsync(userId, token);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Email successfully confirmed for User {UserId}. EventType: {EventType}, LogCategory: {LogCategory}", userId, "EmailChangeConfirmationSuccess", "SecurityAudit");
                    return Ok(new { message = "E-posta adresiniz başarıyla onaylandı ve güncellendi." });
                }
                else
                {
                    var errors = FormatIdentityErrors(result, "E-posta adresi onaylanamadı veya güncellenemedi.");
                    _logger.LogWarning("Email confirmation failed for User {UserId} with Token {Token}. Reason: {@Errors}. EventType: {EventType}, LogCategory: {LogCategory}", userId, token, errors, "EmailChangeConfirmationFailed", "SecurityAudit");
                    return BadRequest(errors);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email confirmation for User {UserId} with Token {Token}. EventType: {EventType}, LogCategory: {LogCategory}", userId, token, "EmailChangeConfirmationError", "SecurityAudit");
                return StatusCode(500, new { message = "E-posta onayı sırasında beklenmedik bir hata oluştu." });
            }
        }

        [HttpPost("me/change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized attempt to change password. No UserId found in claims. EventType: {EventType}, LogCategory: {LogCategory}", "PasswordChangeAttemptUnauthenticated", "SecurityAudit");
                return Unauthorized(new { message = "Kullanıcı kimliği doğrulaması başarısız." });
            }

            if (!ModelState.IsValid)
            {
                var modelErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Invalid model state for ChangePasswordDTO for User {UserId}. Errors: {@ModelStateErrors}. EventType: {EventType}, LogCategory: {LogCategory}", userId, modelErrors, "PasswordChangeInvalidInput", "SecurityAudit");
                return BadRequest(new { message = "Geçersiz istek.", errors = modelErrors });
            }

            _logger.LogInformation("User {UserId} attempting to change password. EventType: {EventType}, LogCategory: {LogCategory}", userId, "UserPasswordChangeAttempt", "SecurityAudit");

            try
            {
                var result = await _authService.ChangePasswordAsync(dto, userId);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User {UserId} successfully changed password. EventType: {EventType}, LogCategory: {LogCategory}", userId, "UserPasswordChangeSuccess", "SecurityAudit");
                    return Ok(new { message = "Şifreniz başarıyla değiştirildi." });
                }
                else
                {
                    var errors = FormatIdentityErrors(result, "Şifre değiştirilemedi.");
                    _logger.LogWarning("Password change failed for User {UserId}. Reason: {@Errors}. EventType: {EventType}, LogCategory: {LogCategory}", userId, errors, "UserPasswordChangeFailure", "SecurityAudit");
                    return BadRequest(errors);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while changing password for User {UserId}. EventType: {EventType}, LogCategory: {LogCategory}", userId, "UserPasswordChangeError", "SecurityAudit");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Şifre değiştirilirken beklenmedik bir hata oluştu." });
            }
        }

        [HttpGet("me/sessions")]
        public async Task<IActionResult> GetActiveSessions([FromHeader(Name = "X-Current-Refresh-Token")] string? currentTokenValue)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Kullanıcı kimliği doğrulaması başarısız." });
            }

            _logger.LogInformation("User {UserId} attempting to retrieve active sessions. CurrentTokenValueProvided: {CurrentTokenValueProvided}", userId, !string.IsNullOrEmpty(currentTokenValue));

            try
            {
                var sessions = await _userSessionService.GetActiveSessionsAsync(userId, currentTokenValue);
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active sessions for User {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Aktif oturumlar alınırken bir hata oluştu." });
            }
        }

        [HttpDelete("me/sessions/{refreshTokenId:int}")]
        public async Task<IActionResult> RevokeSession(int refreshTokenId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Kullanıcı kimliği doğrulaması başarısız." });
            }

            _logger.LogInformation("User {UserId} attempting to revoke session with RefreshTokenId {RefreshTokenId}. EventType: {EventType}, LogCategory: {LogCategory}", userId, refreshTokenId, "UserSessionRevocationAttempt", "SecurityAudit");

            try
            {
                var result = await _userSessionService.RevokeSessionAsync(userId, refreshTokenId);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Session with RefreshTokenId {RefreshTokenId} successfully revoked for User {UserId}. EventType: {EventType}, LogCategory: {LogCategory}", refreshTokenId, userId, "UserSessionRevocationSuccess", "SecurityAudit");
                    return Ok(new { message = "Oturum başarıyla sonlandırıldı." });
                }
                else
                {
                    var errors = FormatIdentityErrors(result, "Oturum sonlandırılamadı.");
                    _logger.LogWarning("Failed to revoke session with RefreshTokenId {RefreshTokenId} for User {UserId}. Reason: {@Errors}. EventType: {EventType}, LogCategory: {LogCategory}", refreshTokenId, userId, errors, "UserSessionRevocationFailure", "SecurityAudit");
                    return BadRequest(errors);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking session with RefreshTokenId {RefreshTokenId} for User {UserId}. EventType: {EventType}, LogCategory: {LogCategory}", refreshTokenId, userId, "UserSessionRevocationError", "SecurityAudit");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Oturum sonlandırılırken beklenmedik bir hata oluştu." });
            }
        }

        [HttpDelete("me/sessions/others")]
        public async Task<IActionResult> RevokeAllOtherSessions([FromHeader(Name = "X-Current-Refresh-Token")] string? currentTokenValue)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Kullanıcı kimliği doğrulaması başarısız." });
            }

            if (string.IsNullOrEmpty(currentTokenValue))
            {
                _logger.LogWarning("User {UserId} attempting to revoke all other sessions but X-Current-Refresh-Token header is missing. EventType: {EventType}, LogCategory: {LogCategory}", userId, "UserBulkSessionRevocationFailure", "SecurityAudit");
                return BadRequest(new { message = "Diğer oturumları sonlandırmak için mevcut oturumun refresh token'ı X-Current-Refresh-Token header'ı ile gönderilmelidir." });
            }

            _logger.LogInformation("User {UserId} attempting to revoke all other sessions, excluding current token. EventType: {EventType}, LogCategory: {LogCategory}", userId, "UserBulkSessionRevocationAttempt", "SecurityAudit");

            try
            {
                var result = await _userSessionService.RevokeAllOtherSessionsAsync(userId, currentTokenValue);
                if (result.Succeeded)
                {
                    _logger.LogInformation("All other sessions successfully revoked for User {UserId}. EventType: {EventType}, LogCategory: {LogCategory}", userId, "UserBulkSessionRevocationSuccess", "SecurityAudit");
                    return Ok(new { message = "Diğer tüm oturumlar başarıyla sonlandırıldı." });
                }
                else
                {
                    var errors = FormatIdentityErrors(result, "Diğer oturumlar sonlandırılamadı.");
                    _logger.LogWarning("Failed to revoke all other sessions for User {UserId}. Reason: {@Errors}. EventType: {EventType}, LogCategory: {LogCategory}", userId, errors, "UserBulkSessionRevocationFailure", "SecurityAudit");
                    return BadRequest(errors);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking all other sessions for User {UserId}. EventType: {EventType}, LogCategory: {LogCategory}", userId, "UserBulkSessionRevocationError", "SecurityAudit");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Diğer oturumlar sonlandırılırken beklenmedik bir hata oluştu." });
            }
        }

        [HttpPost("me/initiate-deletion")]
        public async Task<IActionResult> InitiateDeletion()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized attempt to initiate account deletion. No UserId found in claims.");
                return Unauthorized(new { message = "Kullanıcı kimliği doğrulaması başarısız." });
            }

            _logger.LogInformation("User {UserId} initiating account deletion process.", userId);

            try
            {
                var result = await _accountManagementService.InitiateAccountDeletionAsync(userId);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Account deletion initiation email sent for User {UserId}.", userId);
                    return Ok(new { message = "Hesap silme onay e-postası adresinize gönderildi. Lütfen e-postanızı kontrol edin." });
                }
                else
                {
                    var errors = FormatIdentityErrors(result, "Hesap silme işlemi başlatılamadı.");
                    _logger.LogWarning("Failed to initiate account deletion for User {UserId}. Reason: {@Errors}", userId, errors);
                    return BadRequest(errors);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while initiating account deletion for User {UserId}.", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Hesap silme işlemi başlatılırken beklenmedik bir hata oluştu." });
            }
        }

        [HttpPost("me/resend-deletion-email")]
        public async Task<IActionResult> ResendDeletionEmail()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized attempt to resend deletion email. No UserId found in claims.");
                return Unauthorized(new { message = "Kullanıcı kimliği doğrulaması başarısız." });
            }

            _logger.LogInformation("User {UserId} requesting resend of account deletion email.", userId);

            try
            {
                var result = await _accountManagementService.ResendAccountDeletionEmailAsync(userId);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Account deletion email resent for User {UserId}.", userId);
                    return Ok(new { message = "Hesap silme onay e-postası adresinize yeniden gönderildi." });
                }
                else
                {
                    var errors = FormatIdentityErrors(result, "Onay e-postası yeniden gönderilemedi.");
                    _logger.LogWarning("Failed to resend account deletion email for User {UserId}. Reason: {@Errors}", userId, errors);
                    return BadRequest(errors);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while resending account deletion email for User {UserId}.", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Onay e-postası yeniden gönderilirken beklenmedik bir hata oluştu." });
            }
        }

        [HttpPost("me/confirm-deletion")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmDeletion([FromBody] ConfirmDeletionRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for account deletion confirmation. Token: {Token}, Errors: {@ModelStateErrors}", dto.Token, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(new { message = "Geçersiz istek. Lütfen silme bağlantısını kontrol edin." });
            }

            _logger.LogInformation("Attempting to confirm account deletion with token {Token}.", dto.Token);

            _logger.LogInformation("Account Deletion Survey Data for Token {Token}: Reasons: [{Reasons}], MissingFeatures: [{MissingFeatures}], Feedback: {Feedback}",
                dto.Token,
                dto.DeletionReasons != null && dto.DeletionReasons.Any() ? string.Join(", ", dto.DeletionReasons) : "N/A",
                dto.MissingFeatures != null && dto.MissingFeatures.Any() ? string.Join(", ", dto.MissingFeatures) : "N/A",
                dto.FeedbackText ?? "N/A");

            try
            {
                var (result, userName, email) = await _accountManagementService.ConfirmAccountDeletionAsync(dto.Token);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Account successfully deleted for token {Token} (User: {UserName}, Email: {Email}).", dto.Token, userName, email);
                    return Ok(new { message = "Hesabınız başarıyla silindi.", userName = userName });
                }
                else
                {
                    var errors = FormatIdentityErrors(result, "Hesap silinemedi.");
                    _logger.LogWarning("Failed to confirm account deletion with token {Token}. Reason: {@Errors}", dto.Token, errors);
                    if (result.Errors.Any(e => e.Description == "Geçersiz veya süresi dolmuş silme bağlantısı."))
                    {
                        return NotFound(errors);
                    }
                    return BadRequest(errors);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while confirming account deletion with token {Token}.", dto.Token);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Hesap silinirken beklenmedik bir hata oluştu." });
            }
        }

        private static object FormatIdentityErrors(IdentityResult result, string defaultMessage = "İşlem başarısız oldu.")
        {
            var errorMessages = result.Errors
                .Select(e => e.Code switch
                {
                    "DuplicateUserName" => $"Kullanıcı adı '{e.Description.Split('\'')[1]}' zaten kullanılıyor.",
                    "DuplicateEmail" => $"E-posta adresi '{e.Description.Split('\'')[1]}' zaten kullanılıyor.",
                    "UserNotFound" => "Kullanıcı bulunamadı.",
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

        [HttpPut("me/profile-picture")]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile file)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized attempt to update profile picture. No UserId found in claims.");
                return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı." });
            }

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("User {UserId} attempted to upload an invalid/empty file for profile picture.", userId);
                return BadRequest(new { message = "Geçersiz dosya. Lütfen bir dosya seçin." });
            }

            _logger.LogInformation("User {UserId} attempting to upload profile picture. FileName: {FileName}, Size: {FileSize} bytes", userId, file.FileName, file.Length);

            try
            {
                var imageUrl = await _cloudinaryService.UploadImageAsync(file, "profile_pictures");
                if (string.IsNullOrEmpty(imageUrl))
                {
                    _logger.LogError("Cloudinary upload failed or returned no URL for User {UserId}. FileName: {FileName}", userId, file.FileName);
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Resim yüklenirken bir hata oluştu." });
                }

                _logger.LogInformation("User {UserId} successfully uploaded image to Cloudinary. Url: {ImageUrl}", userId, imageUrl);

                var updateSuccess = await _userProfileService.UpdateProfilePictureAsync(userId, imageUrl);
                if (!updateSuccess)
                {
                    _logger.LogWarning("Failed to update profile picture URL in database for User {UserId}. ImageUrl: {ImageUrl}", userId, imageUrl);
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Profil resmi güncellenirken bir veritabanı hatası oluştu." });
                }

                _logger.LogInformation("Successfully updated profile picture for User {UserId}. New Url: {ImageUrl}", userId, imageUrl);
                return Ok(new { profilePictureUrl = imageUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while User {UserId} was updating profile picture. FileName: {FileName}", userId, file.FileName);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Profil resmi güncellenirken beklenmedik bir hata oluştu." });
            }
        }
    }
}
