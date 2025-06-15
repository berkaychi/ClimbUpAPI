using ClimbUpAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using ClimbUpAPI.Models.DTOs.UsersDTOs;
using ClimbUpAPI.Models.DTOs.SessionDTOs;
using ClimbUpAPI.Data;
using ClimbUpAPI.Models.DTOs.Admin;
using System;
using Microsoft.Extensions.Configuration;

namespace ClimbUpAPI.Services.Implementations
{
    public class UserService : IUserProfileService, IAccountManagementService, IUserSessionService, IAdminUserService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly ILogger<UserService> _logger;
        private readonly IMapper _mapper;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;

        public UserService(
            UserManager<AppUser> userManager,
            ApplicationDbContext context,
            RoleManager<AppRole> roleManager,
            ILogger<UserService> logger,
            IMapper mapper,
            IEmailSender emailSender,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _context = context;
            _roleManager = roleManager;
            _logger = logger;
            _mapper = mapper;
            _emailSender = emailSender;
            _configuration = configuration;
        }

        public async Task<UserGetDTO?> GetUserByIdAsync(string id)
        {
            _logger.LogDebug("Attempting to get user by ID {UserId}", id);
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User not found for ID {UserId}", id);
                return null;
            }
            var userDto = _mapper.Map<UserGetDTO>(user);
            _logger.LogInformation("Successfully retrieved user by ID {UserId}. UserName: {UserName}, Email: {Email}", id, userDto.UserName, userDto.Email);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Full UserGetDTO for ID {UserId}: {@UserGetDTO}", id, userDto);
            }
            return userDto;
        }

        public async Task<UserGetDTO?> GetUserByUserIdAsync(string userId)
        {
            _logger.LogDebug("Attempting to get user by UserId {UserId}", userId);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for UserId {UserId}", userId);
                return null;
            }
            var userDto = _mapper.Map<UserGetDTO>(user);
            _logger.LogInformation("Successfully retrieved user by UserId {UserId}. UserName: {UserName}, Email: {Email}", userId, userDto.UserName, userDto.Email);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Full UserGetDTO for UserId {UserId}: {@UserGetDTO}", userId, userDto);
            }
            return userDto;
        }

        public async Task<List<UserGetDTO>> GetAllUsersAsync()
        {
            _logger.LogDebug("Attempting to get all users.");
            var users = await _userManager.Users
                .Select(u => new UserGetDTO
                {
                    FullName = u.FullName,
                    UserName = u.UserName ?? "",
                    Email = u.Email ?? "",
                    DateAdded = u.DateAdded
                })
                .ToListAsync();
            _logger.LogInformation("Successfully retrieved {UserCount} users.", users.Count);
            if (_logger.IsEnabled(LogLevel.Debug) && users.Any())
            {
                _logger.LogDebug("Retrieved all users: {@UserList}", users);
            }
            return users;
        }

        public async Task<IdentityResult> UpdateProfileAsync(UpdateProfileDTO dto, string userId)
        {
            _logger.LogDebug("User {UserId} attempting to update profile with DTO: {@UpdateProfileDTO}. EventType: {EventType}, LogCategory: {LogCategory}", userId, dto, "UserProfileUpdateAttempt", "SecurityAudit");
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Profile update failed for User {UserId}. Reason: User not found. EventType: {EventType}, LogCategory: {LogCategory}", userId, "UserProfileUpdateFailure", "SecurityAudit");
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });
            }

            user.FullName = dto.FullName;
            user.UserName = dto.UserName;
            if (!string.IsNullOrEmpty(dto.ProfilePictureUrl))
            {
                user.ProfilePictureUrl = dto.ProfilePictureUrl;
            }

            IdentityResult result;

            if (!string.IsNullOrEmpty(dto.Email) && !string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("User {UserId} attempting to change email from {OldEmail} to {NewEmail}. EventType: {EventType}, LogCategory: {LogCategory}", userId, user.Email, dto.Email, "EmailChangeAttempt", "SecurityAudit");

                var existingUserWithNewEmail = await _userManager.FindByEmailAsync(dto.Email);
                if (existingUserWithNewEmail != null && existingUserWithNewEmail.Id != userId)
                {
                    _logger.LogWarning("User {UserId} failed to change email to {NewEmail}. Reason: Email already in use by another user. EventType: {EventType}, LogCategory: {LogCategory}", userId, dto.Email, "EmailChangeFailure", "SecurityAudit");
                    return IdentityResult.Failed(new IdentityError { Code = "DuplicateEmail", Description = "Bu e-posta adresi zaten başka bir kullanıcı tarafından kullanılıyor." });
                }

                user.PendingNewEmail = dto.Email;
                user.PendingEmailChangeToken = Guid.NewGuid().ToString("N");
                user.PendingEmailChangeTokenExpiration = DateTime.UtcNow.AddHours(Convert.ToInt32(_configuration["AuthSettings:ChangeEmailTokenLifespanHours"] ?? "24"));

                var confirmationLink = $"{_configuration["AppBaseUrl"]?.TrimEnd('/')}/confirm-email-change?userId={user.Id}&token={user.PendingEmailChangeToken}";

                var emailSubject = "E-posta Adresi Değişikliği Onayı - ClimbUp";
                var emailBody = $"Merhaba {user.UserName ?? "Climber"},<br><br>" +
                                $"E-posta adresinizi değiştirmek için bir talepte bulundunuz. Yeni e-posta adresiniz: {user.PendingNewEmail}.<br>" +
                                $"Bu değişikliği onaylamak için lütfen aşağıdaki bağlantıya tıklayın. Bu bağlantı {user.PendingEmailChangeTokenExpiration:dd.MM.yyyy HH:mm} tarihine kadar geçerlidir:<br>" +
                                $"<a href='{confirmationLink}'>E-posta Adresimi Onayla</a><br><br>" +
                                "Eğer bu talebi siz yapmadıysanız, bu e-postayı görmezden gelebilirsiniz.<br><br>" +
                                "Teşekkürler,<br>ClimbUp Ekibi";

                try
                {
                    await _emailSender.SendEmailAsync(user.PendingNewEmail, emailSubject, emailBody);
                    _logger.LogInformation("Email change confirmation email sent to {NewEmail} for User {UserId}. Token: {Token}. EventType: {EventType}, LogCategory: {LogCategory}", user.PendingNewEmail, userId, user.PendingEmailChangeToken, "EmailChangeConfirmationSent", "SecurityAudit");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email change confirmation to {NewEmail} for User {UserId}. EventType: {EventType}, LogCategory: {LogCategory}", user.PendingNewEmail, userId, "EmailChangeConfirmationSendFailure", "SecurityAudit");
                }
                result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User {UserId} profile updated (FullName/UserName) and email change initiated to {NewEmail}. Confirmation required. EventType: {EventType}, LogCategory: {LogCategory}", userId, user.PendingNewEmail, "UserProfileUpdateWithEmailChangeInitiated", "SecurityAudit");
                }
            }
            else
            {
                result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User {UserId} successfully updated their profile (no email change). EventType: {EventType}, LogCategory: {LogCategory}", userId, "UserProfileUpdateSuccess", "SecurityAudit");
                }
            }

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => new { e.Code, e.Description }).ToList();
                _logger.LogWarning("Profile update failed for User {UserId}. Errors: {@Errors}. EventType: {EventType}, LogCategory: {LogCategory}", userId, errors, "UserProfileUpdateFailure", "SecurityAudit");
            }
            return result;
        }

        public async Task<IdentityResult> ConfirmEmailChangeAsync(string userId, string token)
        {
            _logger.LogDebug("User {UserId} attempting to confirm email change with token {Token}. EventType: {EventType}, LogCategory: {LogCategory}", userId, token, "EmailChangeConfirmAttempt", "SecurityAudit");

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("ConfirmEmailChangeAsync failed. UserId or Token is null or empty. UserId: {UserId}, Token: {Token}. EventType: {EventType}, LogCategory: {LogCategory}", userId, token, "EmailChangeConfirmFailure", "SecurityAudit");
                return IdentityResult.Failed(new IdentityError { Code = "InvalidParameters", Description = "Kullanıcı ID'si veya token geçersiz." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("ConfirmEmailChangeAsync failed for User {UserId}. Reason: User not found. Token: {Token}. EventType: {EventType}, LogCategory: {LogCategory}", userId, token, "EmailChangeConfirmFailure", "SecurityAudit");
                return IdentityResult.Failed(new IdentityError { Code = "UserNotFound", Description = "Kullanıcı bulunamadı." });
            }

            if (user.PendingEmailChangeToken != token)
            {
                _logger.LogWarning("ConfirmEmailChangeAsync failed for User {UserId}. Reason: Invalid token. Provided Token: {ProvidedToken}, Expected Token: {ExpectedToken}. EventType: {EventType}, LogCategory: {LogCategory}", userId, token, user.PendingEmailChangeToken, "EmailChangeConfirmFailure", "SecurityAudit");
                return IdentityResult.Failed(new IdentityError { Code = "InvalidToken", Description = "Geçersiz onay token'ı." });
            }

            if (user.PendingEmailChangeTokenExpiration == null || user.PendingEmailChangeTokenExpiration.Value < DateTime.UtcNow)
            {
                _logger.LogWarning("ConfirmEmailChangeAsync failed for User {UserId}. Reason: Expired token. Expiration: {ExpirationDate}. EventType: {EventType}, LogCategory: {LogCategory}", userId, user.PendingEmailChangeTokenExpiration, "EmailChangeConfirmFailure", "SecurityAudit");
                return IdentityResult.Failed(new IdentityError { Code = "ExpiredToken", Description = "Onay token'ının süresi dolmuş." });
            }

            if (string.IsNullOrEmpty(user.PendingNewEmail))
            {
                _logger.LogWarning("ConfirmEmailChangeAsync failed for User {UserId}. Reason: PendingNewEmail is not set. EventType: {EventType}, LogCategory: {LogCategory}", userId, "EmailChangeConfirmFailure", "SecurityAudit");
                return IdentityResult.Failed(new IdentityError { Code = "PendingEmailMissing", Description = "Değiştirilecek yeni e-posta adresi bulunamadı." });
            }

            var existingUserWithNewEmail = await _userManager.FindByEmailAsync(user.PendingNewEmail);
            if (existingUserWithNewEmail != null && existingUserWithNewEmail.Id != userId)
            {
                _logger.LogWarning("User {UserId} failed to confirm email change to {NewEmail}. Reason: Email already in use by another user. EventType: {EventType}, LogCategory: {LogCategory}", userId, user.PendingNewEmail, "EmailChangeConfirmFailure", "SecurityAudit");
                user.PendingNewEmail = null;
                user.PendingEmailChangeToken = null;
                user.PendingEmailChangeTokenExpiration = null;
                await _userManager.UpdateAsync(user);
                return IdentityResult.Failed(new IdentityError { Code = "DuplicateEmailOnConfirm", Description = "Bu e-posta adresi onay sırasında başka bir kullanıcı tarafından alınmış." });
            }

            var oldEmail = user.Email;
            var newEmail = user.PendingNewEmail;

            var setEmailResult = await _userManager.SetEmailAsync(user, newEmail);
            if (!setEmailResult.Succeeded)
            {
                _logger.LogWarning("ConfirmEmailChangeAsync: SetEmailAsync failed for User {UserId} to {NewEmail}. Errors: {@Errors}. EventType: {EventType}, LogCategory: {LogCategory}", userId, newEmail, setEmailResult.Errors.Select(e => e.Description), "EmailChangeConfirmFailure", "SecurityAudit");
                return setEmailResult;
            }

            var setUserNameResult = await _userManager.SetUserNameAsync(user, newEmail);
            if (!setUserNameResult.Succeeded)
            {
                _logger.LogWarning("ConfirmEmailChangeAsync: SetUserNameAsync failed for User {UserId} to {NewEmail} after email was set. Errors: {@Errors}. EventType: {EventType}, LogCategory: {LogCategory}", userId, newEmail, setUserNameResult.Errors.Select(e => e.Description), "EmailChangeConfirmFailure", "SecurityAudit");
                await _userManager.SetEmailAsync(user, oldEmail);
                return setUserNameResult;
            }

            user.EmailConfirmed = true;
            user.PendingNewEmail = null;
            user.PendingEmailChangeToken = null;
            user.PendingEmailChangeTokenExpiration = null;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogWarning("ConfirmEmailChangeAsync: Final UpdateAsync failed for User {UserId} after email/username change. Errors: {@Errors}. EventType: {EventType}, LogCategory: {LogCategory}", userId, updateResult.Errors.Select(e => e.Description), "EmailChangeConfirmFailure", "SecurityAudit");
                return updateResult;
            }

            await _userManager.UpdateSecurityStampAsync(user);

            _logger.LogInformation("User {UserId} successfully confirmed email change from {OldEmail} to {NewEmail}. EventType: {EventType}, LogCategory: {LogCategory}", userId, oldEmail, newEmail, "EmailChangeConfirmSuccess", "SecurityAudit");
            return IdentityResult.Success;
        }

        public async Task<List<ActiveSessionDto>> GetActiveSessionsAsync(string userId, string? currentTokenValue = null)
        {
            _logger.LogDebug("User {UserId} attempting to get active sessions. CurrentTokenProvided: {CurrentTokenProvided}", userId, !string.IsNullOrEmpty(currentTokenValue));

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("GetActiveSessionsAsync failed for User {UserId}. Reason: User not found.", userId);
                return new List<ActiveSessionDto>();
            }

            var activeSessions = await _context.UserRefreshTokens
                .Where(rt => rt.UserId == userId && rt.Revoked == null && rt.Expires > DateTime.UtcNow)
                .OrderByDescending(rt => rt.Created)
                .Select(rt => new ActiveSessionDto
                {
                    Id = rt.Id,
                    DeviceBrowserInfo = rt.DeviceBrowserInfo,
                    IpAddress = rt.IpAddress,
                    Created = rt.Created,
                    Expires = rt.Expires,
                    LastUsedDate = rt.LastUsedDate,
                    IsCurrentSession = !string.IsNullOrEmpty(currentTokenValue) && rt.Token == currentTokenValue
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {SessionCount} active sessions for User {UserId}", activeSessions.Count, userId);
            return activeSessions;
        }

        public async Task<IdentityResult> RevokeSessionAsync(string userId, int refreshTokenId)
        {
            _logger.LogDebug("User {UserId} attempting to revoke session (RefreshTokenId: {RefreshTokenId}). EventType: {EventType}, LogCategory: {LogCategory}", userId, refreshTokenId, "SessionRevocationAttempt", "SecurityAudit");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("RevokeSessionAsync failed for User {UserId}. Reason: User not found. RefreshTokenId: {RefreshTokenId}. EventType: {EventType}, LogCategory: {LogCategory}", userId, refreshTokenId, "SessionRevocationFailure", "SecurityAudit");
                return IdentityResult.Failed(new IdentityError { Code = "UserNotFound", Description = "Kullanıcı bulunamadı." });
            }

            var refreshToken = await _context.UserRefreshTokens
                .FirstOrDefaultAsync(rt => rt.Id == refreshTokenId && rt.UserId == userId);

            if (refreshToken == null)
            {
                _logger.LogWarning("RevokeSessionAsync failed for User {UserId}. Reason: Refresh token not found or does not belong to user. RefreshTokenId: {RefreshTokenId}. EventType: {EventType}, LogCategory: {LogCategory}", userId, refreshTokenId, "SessionRevocationFailure", "SecurityAudit");
                return IdentityResult.Failed(new IdentityError { Code = "TokenNotFound", Description = "Oturum token'ı bulunamadı." });
            }

            if (refreshToken.IsRevoked || refreshToken.IsExpired)
            {
                _logger.LogInformation("RevokeSessionAsync for User {UserId}, RefreshTokenId {RefreshTokenId}: Token already inactive. EventType: {EventType}, LogCategory: {LogCategory}", userId, refreshTokenId, "SessionRevocationNoAction", "SecurityAudit");
                return IdentityResult.Success;

            }
            refreshToken.Revoked = DateTime.UtcNow;
            _context.UserRefreshTokens.Update(refreshToken);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Session (RefreshTokenId: {RefreshTokenId}) successfully revoked for User {UserId}. EventType: {EventType}, LogCategory: {LogCategory}", refreshTokenId, userId, "SessionRevocationSuccess", "SecurityAudit");
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> RevokeAllOtherSessionsAsync(string userId, string currentTokenValue)
        {
            _logger.LogDebug("User {UserId} attempting to revoke all other sessions, excluding current token (if provided). CurrentTokenValueProvided: {CurrentTokenValueProvided}. EventType: {EventType}, LogCategory: {LogCategory}", userId, !string.IsNullOrEmpty(currentTokenValue), "BulkSessionRevocationAttempt", "SecurityAudit");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("RevokeAllOtherSessionsAsync failed for User {UserId}. Reason: User not found. EventType: {EventType}, LogCategory: {LogCategory}", userId, "BulkSessionRevocationFailure", "SecurityAudit");
                return IdentityResult.Failed(new IdentityError { Code = "UserNotFound", Description = "Kullanıcı bulunamadı." });
            }

            var activeRefreshTokens = await _context.UserRefreshTokens
                .Where(rt => rt.UserId == userId && rt.Revoked == null && rt.Expires > DateTime.UtcNow)
                .ToListAsync();

            if (!activeRefreshTokens.Any())
            {
                _logger.LogInformation("No active sessions found to revoke for User {UserId}. EventType: {EventType}, LogCategory: {LogCategory}", userId, "BulkSessionRevocationNoAction", "SecurityAudit");
                return IdentityResult.Success;
            }

            int revokedCount = 0;
            foreach (var rt in activeRefreshTokens)
            {
                if (rt.Token != currentTokenValue)
                {
                    rt.Revoked = DateTime.UtcNow;
                    _context.UserRefreshTokens.Update(rt);
                    revokedCount++;
                }
            }

            if (revokedCount > 0)
            {
                await _context.SaveChangesAsync();
                await _userManager.UpdateSecurityStampAsync(user);
                _logger.LogInformation("{RevokedCount} other sessions successfully revoked for User {UserId}. Security stamp updated. EventType: {EventType}, LogCategory: {LogCategory}", revokedCount, userId, "BulkSessionRevocationSuccess", "SecurityAudit");
            }
            else
            {
                _logger.LogInformation("No other active sessions found to revoke for User {UserId} (excluding current if provided). EventType: {EventType}, LogCategory: {LogCategory}", userId, "BulkSessionRevocationNoAction", "SecurityAudit");
            }

            return IdentityResult.Success;
        }

        private async Task<IdentityResult> PerformActualUserDeletionAsync(AppUser user, string logPrefix = "")
        {
            var userId = user.Id;
            _logger.LogInformation("{LogPrefix}Performing actual deletion for User {UserId}.", logPrefix, userId);

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("{LogPrefix}User {UserId} successfully deleted from UserManager.", logPrefix, userId);
            }
            else
            {
                var errors = result.Errors.Select(e => new { e.Code, e.Description }).ToList();
                _logger.LogWarning("{LogPrefix}UserManager failed to delete User {UserId}. Errors: {@Errors}", logPrefix, userId, errors);
            }
            return result;
        }

        public async Task<IdentityResult> InitiateAccountDeletionAsync(string userId)
        {
            var configuredBaseUrl = _configuration["AppBaseUrl"];
            _logger.LogInformation("AppBaseUrl from configuration: {AppBaseUrl}", configuredBaseUrl);

            _logger.LogDebug("User {UserId} initiating account deletion process.", userId);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.Email == null)
            {
                _logger.LogWarning("Cannot initiate account deletion for User {UserId}. User not found or email is missing.", userId);
                return IdentityResult.Failed(new IdentityError { Description = "Kullanıcı bulunamadı veya e-posta adresi kayıtlı değil." });
            }

            var existingRequest = await _context.AccountDeletionRequests
                .FirstOrDefaultAsync(r => r.UserId == userId && !r.IsConfirmed && r.ExpirationDate > DateTime.UtcNow);

            if (existingRequest != null)
            {
                _logger.LogInformation("User {UserId} already has an active account deletion request. Resending email for token {Token}.", userId, existingRequest.Token);
                return IdentityResult.Failed(new IdentityError { Description = "Zaten aktif bir hesap silme talebiniz bulunmaktadır. Lütfen e-postanızı kontrol edin veya yeniden gönderme seçeneğini kullanın." });
            }


            var token = Guid.NewGuid().ToString("N");
            var expirationDate = DateTime.UtcNow.AddHours(24);

            var deletionRequest = new AccountDeletionRequest
            {
                UserId = userId,
                Token = token,
                ExpirationDate = expirationDate,
                IsConfirmed = false,
                RequestDate = DateTime.UtcNow
            };

            await _context.AccountDeletionRequests.AddAsync(deletionRequest);
            await _context.SaveChangesAsync();

            var baseUrl = _configuration["AppBaseUrl"];
            if (string.IsNullOrEmpty(baseUrl))
            {
                _logger.LogError("AppBaseUrl is not configured. Cannot create confirmation link for account deletion initiation.");
                _context.AccountDeletionRequests.Remove(deletionRequest);
                await _context.SaveChangesAsync();
                return IdentityResult.Failed(new IdentityError { Description = "Uygulama yapılandırma hatası nedeniyle onay linki oluşturulamadı." });
            }
            var confirmationLink = $"{baseUrl.TrimEnd('/')}/confirm-delete-account?token={token}";

            var emailSubject = "Hesap Silme Onayı - ClimbUp";
            var emailBody = $"Merhaba {user.UserName ?? "Climber"},<br><br>" +
                            $"Hesabınızı silme talebinizi aldık. Bu işlemi onaylamak için lütfen aşağıdaki bağlantıya tıklayın. Bu bağlantı 24 saat geçerlidir:<br>" +
                            $"<a href='{confirmationLink}'>Hesabımı Sil</a><br><br>" +
                            "Eğer bu talebi siz yapmadıysanız, bu e-postayı görmezden gelebilirsiniz.<br><br>" +
                            "Teşekkürler,<br>ClimbUp Ekibi";

            try
            {
                await _emailSender.SendEmailAsync(user.Email, emailSubject, emailBody);
                _logger.LogInformation("Account deletion confirmation email sent to User {UserId} at {Email}. Token: {Token}", userId, user.Email, token);
                return IdentityResult.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send account deletion email to User {UserId} at {Email}.", userId, user.Email);
                _context.AccountDeletionRequests.Remove(deletionRequest);
                await _context.SaveChangesAsync();
                return IdentityResult.Failed(new IdentityError { Description = "Hesap silme onay e-postası gönderilemedi." });
            }
        }

        public async Task<(IdentityResult result, string? userName, string? email)> ConfirmAccountDeletionAsync(string token)
        {
            _logger.LogDebug("Attempting to confirm account deletion with token {Token}.", token);

            var deletionRequest = await _context.AccountDeletionRequests
                .Include(dr => dr.User)
                .FirstOrDefaultAsync(r => r.Token == token && !r.IsConfirmed && r.ExpirationDate > DateTime.UtcNow);

            if (deletionRequest == null || deletionRequest.User == null)
            {
                _logger.LogWarning("Invalid or expired account deletion token {Token} or user not found.", token);
                return (IdentityResult.Failed(new IdentityError { Description = "Geçersiz veya süresi dolmuş silme bağlantısı." }), null, null);
            }

            var user = deletionRequest.User;
            var userId = user.Id;
            var userName = user.UserName;
            var userEmail = user.Email;

            _logger.LogInformation("Token {Token} validated for User {UserId}. Proceeding with account deletion.", token, userId);

            var deletionResult = await PerformActualUserDeletionAsync(user, "[ConfirmAccountDeletion] ");

            if (deletionResult.Succeeded)
            {
                deletionRequest.IsConfirmed = true;
                deletionRequest.ConfirmationDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Account for User {UserId} (Token: {Token}) successfully deleted and request marked as confirmed.", userId, token);
                return (IdentityResult.Success, userName, userEmail);
            }
            else
            {
                _logger.LogWarning("Failed to delete account for User {UserId} (Token: {Token}) after token confirmation. Errors: {@Errors}", userId, token, deletionResult.Errors);
                return (deletionResult, userName, userEmail);
            }
        }

        public async Task<IdentityResult> ResendAccountDeletionEmailAsync(string userId)
        {
            _logger.LogDebug("User {UserId} requesting resend of account deletion email.", userId);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.Email == null)
            {
                _logger.LogWarning("Cannot resend deletion email for User {UserId}. User not found or email is missing.", userId);
                return IdentityResult.Failed(new IdentityError { Description = "Kullanıcı bulunamadı veya e-posta adresi kayıtlı değil." });
            }

            var activeRequest = await _context.AccountDeletionRequests
                .FirstOrDefaultAsync(r => r.UserId == userId && !r.IsConfirmed && r.ExpirationDate > DateTime.UtcNow);

            if (activeRequest == null)
            {
                _logger.LogWarning("No active, unconfirmed deletion request found for User {UserId} to resend email.", userId);
                return IdentityResult.Failed(new IdentityError { Description = "Aktif bir hesap silme talebiniz bulunmuyor veya talebinizin süresi dolmuş. Lütfen yeni bir talep başlatın." });
            }

            var baseUrl = _configuration["AppBaseUrl"];
            if (string.IsNullOrEmpty(baseUrl))
            {
                _logger.LogError("AppBaseUrl is not configured. Cannot create confirmation link for resending deletion email.");
                return IdentityResult.Failed(new IdentityError { Description = "Uygulama yapılandırma hatası nedeniyle onay linki oluşturulamadı." });
            }
            var confirmationLink = $"{baseUrl.TrimEnd('/')}/confirm-delete-account?token={activeRequest.Token}";

            var emailSubject = "Hesap Silme Onayı (Yeniden Gönderim) - ClimbUp";
            var emailBody = $"Merhaba {user.UserName ?? "Climber"},<br><br>" +
                            $"Hesabınızı silme talebiniz için onay e-postasını yeniden gönderiyoruz. Bu işlemi onaylamak için lütfen aşağıdaki bağlantıya tıklayın. Bu bağlantı {activeRequest.ExpirationDate:dd.MM.yyyy HH:mm} tarihine kadar geçerlidir:<br>" +
                            $"<a href='{confirmationLink}'>Hesabımı Sil</a><br><br>" +
                            "Eğer bu talebi siz yapmadıysanız, bu e-postayı görmezden gelebilirsiniz.<br><br>" +
                            "Teşekkürler,<br>ClimbUp Ekibi";
            try
            {
                await _emailSender.SendEmailAsync(user.Email, emailSubject, emailBody);
                _logger.LogInformation("Account deletion confirmation email resent to User {UserId} at {Email}. Token: {Token}", userId, user.Email, activeRequest.Token);
                return IdentityResult.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend account deletion email to User {UserId} at {Email}.", userId, user.Email);
                return IdentityResult.Failed(new IdentityError { Description = "Hesap silme onay e-postası yeniden gönderilemedi." });
            }
        }

        public async Task<IdentityResult> DeleteUserByAdminAsync(string userId)
        {
            _logger.LogDebug("Admin attempting to delete User {UserId}. EventType: {EventType}, LogCategory: {LogCategory}", userId, "AdminAccountDeletionAttempt", "SecurityAudit");
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Admin account deletion failed for User {UserId}. Reason: User not found. EventType: {EventType}, LogCategory: {LogCategory}", userId, "AdminAccountDeletionFailure", "SecurityAudit");
                return IdentityResult.Failed(new IdentityError { Description = "Kullanıcı bulunamadı." });
            }

            _logger.LogInformation("Admin proceeding with account deletion for User {UserId}.", userId);

            return await PerformActualUserDeletionAsync(user, "[AdminDeletion] ");
        }

        public async Task<List<AdminUserListDto>> GetUsersForAdminAsync()
        {
            _logger.LogDebug("Attempting to get all users for admin panel.");
            var users = await _userManager.Users.ToListAsync();
            var adminUserList = new List<AdminUserListDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                adminUserList.Add(new AdminUserListDto
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName,
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnd = user.LockoutEnd,
                    Roles = [.. roles],
                    DateAdded = user.DateAdded
                });
            }
            _logger.LogInformation("Successfully retrieved {UserCount} users for admin panel.", adminUserList.Count);
            if (_logger.IsEnabled(LogLevel.Debug) && adminUserList.Count != 0)
            {
                _logger.LogDebug("Retrieved users for admin panel: {@AdminUserList}", adminUserList);
            }
            return adminUserList;
        }
        public async Task<IdentityResult> AssignRoleAsync(string userId, string roleName)
        {
            _logger.LogDebug("Attempting to assign role {RoleName} to User {TargetUserId}. EventType: {EventType}, LogCategory: {LogCategory}", roleName, userId, "AdminRoleAssignmentAttempt", "SecurityAudit");
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Failed to assign role {RoleName} to User {TargetUserId}. Reason: User not found. EventType: {EventType}, LogCategory: {LogCategory}", roleName, userId, "AdminRoleAssignmentFailure", "SecurityAudit");
                return IdentityResult.Failed(new IdentityError { Description = $"User with ID {userId} not found." });
            }

            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                _logger.LogWarning("Failed to assign role {RoleName} to User {TargetUserId}. Reason: Role not found. EventType: {EventType}, LogCategory: {LogCategory}", roleName, userId, "AdminRoleAssignmentFailure", "SecurityAudit");
                return IdentityResult.Failed(new IdentityError { Description = $"Role '{roleName}' not found." });
            }

            if (await _userManager.IsInRoleAsync(user, roleName))
            {
                _logger.LogWarning("Failed to assign role {RoleName} to User {TargetUserId}. Reason: User already in role. EventType: {EventType}, LogCategory: {LogCategory}", roleName, userId, "AdminRoleAssignmentFailure", "SecurityAudit");
                return IdentityResult.Failed(new IdentityError { Description = $"User already has the role '{roleName}'." });
            }

            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (result.Succeeded)
            {
                _logger.LogInformation("Successfully assigned role {RoleName} to User {TargetUserId}. EventType: {EventType}, LogCategory: {LogCategory}", roleName, userId, "AdminRoleAssignmentSuccess", "SecurityAudit");
                await _userManager.UpdateSecurityStampAsync(user);
            }
            else
            {
                var errors = result.Errors.Select(e => new { e.Code, e.Description }).ToList();
                _logger.LogWarning("Failed to assign role {RoleName} to User {TargetUserId}. Errors: {@Errors}. EventType: {EventType}, LogCategory: {LogCategory}", roleName, userId, errors, "AdminRoleAssignmentFailure", "SecurityAudit");
            }
            return result;
        }

        public async Task<IdentityResult> RemoveRoleAsync(string userId, string roleName)
        {
            _logger.LogDebug("Attempting to remove role {RoleName} from User {TargetUserId}. EventType: {EventType}, LogCategory: {LogCategory}", roleName, userId, "AdminRoleRemovalAttempt", "SecurityAudit");
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Failed to remove role {RoleName} from User {TargetUserId}. Reason: User not found. EventType: {EventType}, LogCategory: {LogCategory}", roleName, userId, "AdminRoleRemovalFailure", "SecurityAudit");
                return IdentityResult.Failed(new IdentityError { Description = $"User with ID {userId} not found." });
            }

            if (!await _userManager.IsInRoleAsync(user, roleName))
            {
                _logger.LogWarning("Failed to remove role {RoleName} from User {TargetUserId}. Reason: User not in role. EventType: {EventType}, LogCategory: {LogCategory}", roleName, userId, "AdminRoleRemovalFailure", "SecurityAudit");
                return IdentityResult.Failed(new IdentityError { Description = $"User does not have the role '{roleName}'." });
            }

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            if (result.Succeeded)
            {
                _logger.LogInformation("Successfully removed role {RoleName} from User {TargetUserId}. EventType: {EventType}, LogCategory: {LogCategory}", roleName, userId, "AdminRoleRemovalSuccess", "SecurityAudit");
                await _userManager.UpdateSecurityStampAsync(user);
            }
            else
            {
                var errors = result.Errors.Select(e => new { e.Code, e.Description }).ToList();
                _logger.LogWarning("Failed to remove role {RoleName} from User {TargetUserId}. Errors: {@Errors}. EventType: {EventType}, LogCategory: {LogCategory}", roleName, userId, errors, "AdminRoleRemovalFailure", "SecurityAudit");
            }
            return result;
        }


        public async Task<IList<string>> GetUserRolesAsync(string userId)
        {
            _logger.LogDebug("Attempting to get roles for User {UserId}.", userId);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Cannot get roles for User {UserId}. User not found.", userId);
                return new List<string>();
            }
            var roles = await _userManager.GetRolesAsync(user);
            _logger.LogInformation("Successfully retrieved {RoleCount} roles for User {UserId}.", roles.Count, userId);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Roles for User {UserId}: {@Roles}", userId, roles);
            }
            return roles;
        }

        public async Task<bool> UpdateProfilePictureAsync(string userId, string profilePictureUrl)
        {
            _logger.LogDebug("Attempting to update profile picture for User {UserId} to {ProfilePictureUrl}. EventType: {EventType}, LogCategory: {LogCategory}", userId, profilePictureUrl, "ProfilePictureUpdateAttempt", "SecurityAudit");
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Profile picture update failed for User {UserId}. Reason: User not found. EventType: {EventType}, LogCategory: {LogCategory}", userId, "ProfilePictureUpdateFailure", "SecurityAudit");
                return false;
            }

            user.ProfilePictureUrl = profilePictureUrl;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                _logger.LogInformation("Profile picture updated successfully for User {UserId}. EventType: {EventType}, LogCategory: {LogCategory}", userId, "ProfilePictureUpdateSuccess", "SecurityAudit");
            }
            else
            {
                var errors = result.Errors.Select(e => new { e.Code, e.Description }).ToList();
                _logger.LogWarning("Profile picture update failed for User {UserId}. Errors: {@Errors}. EventType: {EventType}, LogCategory: {LogCategory}", userId, errors, "ProfilePictureUpdateFailure", "SecurityAudit");
            }
            return result.Succeeded;
        }
    }
}
