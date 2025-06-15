using ClimbUpAPI.Models;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Cryptography;
using ClimbUpAPI.Models.DTOs.UsersDTOs;
using ClimbUpAPI.Data;

namespace ClimbUpAPI.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IConfiguration _config;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AuthService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly ITaskService _taskService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IConfiguration config,
            IEmailSender emailSender,
            ILogger<AuthService> logger,
            ApplicationDbContext context,
            ITaskService taskService,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _emailSender = emailSender;
            _logger = logger;
            _context = context;
            _taskService = taskService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IdentityResult> RegisterAsync(UserDTO dto)
        {
            _logger.LogDebug("Registration attempt for UserName {UserName}, Email {Email}. EventType: {EventType}, LogCategory: {LogCategory}", dto.UserName, dto.Email, "UserRegistrationAttempt", "SecurityAudit");
            var user = new AppUser
            {
                FullName = dto.FullName,
                UserName = dto.UserName,
                Email = dto.Email,
                DateAdded = DateTime.UtcNow,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserId} (UserName: {UserName}) registered successfully. EventType: {EventType}, LogCategory: {LogCategory}", user.Id, user.UserName, "UserRegistrationSuccess", "SecurityAudit");
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = WebUtility.UrlEncode(token);
                var baseUrl = _config["AppBaseUrl"];
                if (string.IsNullOrEmpty(baseUrl))
                {
                    _logger.LogError("AppBaseUrl is not configured. Cannot create email confirmation link for User {UserId}.", user.Id);
                }
                var confirmationLink = $"{baseUrl?.TrimEnd('/')}/confirm-email?userId={user.Id}&token={encodedToken}";

                var emailSubject = "ClimbUp'a Hoş Geldin, Zirve Yolculuğun Başlıyor!";
                var emailBody = $"Merhaba {user.UserName ?? "Climber"},<br><br>" +
                                $"ClimbUp ailesine hoş geldin! Zirveye giden yolda sana eşlik etmek için sabırsızlanıyoruz.<br><br>" +
                                $"Hesabını doğrulamak ve tırmanışına başlamak için lütfen aşağıdaki bağlantıya tıkla:<br>" +
                                $"<a href='{confirmationLink}'>E-posta Adresimi Onayla</a><br><br>" +
                                $"Eğer bu kaydı sen yapmadıysan, bu e-postayı görmezden gelebilirsin.<br><br>" +
                                $"Harika odaklanmalar dileriz!<br>" +
                                $"ClimbUp Ekibi";

                await _emailSender.SendEmailAsync(user.Email ?? "", emailSubject, emailBody);
                _logger.LogInformation("Confirmation email sent to {Email} for User {UserId}. EventType: {EventType}, LogCategory: {LogCategory}", user.Email, user.Id, "EmailConfirmationSent", "SecurityAudit");

                try
                {
                    _logger.LogInformation("Attempting to assign initial tasks for new user {UserId}", user.Id);
                    var activeAppTasks = await _context.AppTasks.Where(t => t.IsActive).ToListAsync();
                    if (activeAppTasks.Any())
                    {
                        await _taskService.AssignOrRefreshTasksAsync(user.Id, activeAppTasks);
                        _logger.LogInformation("Successfully initiated assignment of initial tasks for new user {UserId}", user.Id);
                    }
                    else
                    {
                        _logger.LogInformation("No active AppTask definitions found. Skipping initial task assignment for new user {UserId}", user.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error assigning initial tasks for new user {UserId}. This will not fail the registration. Tasks can be assigned by the background service.", user.Id);
                }
            }
            else
            {
                var errors = result.Errors.Select(e => new { e.Code, e.Description }).ToList();
                _logger.LogWarning("Registration failed for UserName {UserName}, Email {Email}. Errors: {@Errors}. EventType: {EventType}, LogCategory: {LogCategory}", dto.UserName, dto.Email, errors, "UserRegistrationFailure", "SecurityAudit");
            }

            return result;
        }

        public async Task<LoginResponseDTO> LoginAsync(LoginDTO dto)
        {
            _logger.LogDebug("Login attempt for {LoginIdentifier}. EventType: {EventType}, LogCategory: {LogCategory}", dto.Email, "LoginAttempt", "SecurityAudit");
            var isEmail = new EmailAddressAttribute().IsValid(dto.Email);
            var user = isEmail
                ? await _userManager.FindByEmailAsync(dto.Email)
                : await _userManager.FindByNameAsync(dto.Email);

            if (user == null)
            {
                _logger.LogWarning("Login failed for {LoginIdentifier}. EventType: {EventType}, FailureReason: {FailureReason}, LogCategory: {LogCategory}", dto.Email, "LoginFailure", "UserNotFound", "SecurityAudit");
                throw new UnauthorizedAccessException("Geçersiz e-posta/kullanıcı adı veya şifre.");
            }
            _logger.LogDebug("User {UserId} (UserName: {UserName}) found for login attempt. EventType: {EventType}, LogCategory: {LogCategory}", user.Id, user.UserName, "LoginUserFound", "SecurityAudit");

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                _logger.LogWarning("Login failed for User {UserId}. EventType: {EventType}, FailureReason: {FailureReason}, LogCategory: {LogCategory}", user.Id, "LoginFailure", "EmailNotConfirmed", "SecurityAudit");
                throw new UnauthorizedAccessException("Lütfen önce e-posta adresinizi onaylayın.");
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning("Login failed for User {UserId}. EventType: {EventType}, FailureReason: {FailureReason}, LockoutEnd: {LockoutEnd}, LogCategory: {LogCategory}", user.Id, "LoginFailure", "AccountLockedOut", user.LockoutEnd, "SecurityAudit");
                throw new UnauthorizedAccessException("Hesabınız geçici olarak kilitlenmiştir.");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("Login successful for User {UserId}. EventType: {EventType}, LogCategory: {LogCategory}", user.Id, "LoginSuccess", "SecurityAudit");
                await _userManager.ResetAccessFailedCountAsync(user);

                var accessToken = await GenerateJwt(user);
                var refreshToken = GenerateRefreshToken(user.Id);

                await _context.SaveChangesAsync();
                _logger.LogInformation("Tokens generated and saved for User {UserId}. EventType: {EventType}, AccessTokenExpiry: {AccessTokenExpiry}, RefreshTokenId: {RefreshTokenId}, LogCategory: {LogCategory}", user.Id, "TokenIssued", DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["JwtSettings:ExpiryMinutes"] ?? "60")), refreshToken.Id, "SecurityAudit");

                try
                {
                    _logger.LogInformation("Attempting to assign/refresh tasks for user {UserId} on login.", user.Id);
                    var activeAppTasks = await _context.AppTasks.Where(t => t.IsActive).ToListAsync();
                    if (activeAppTasks.Any())
                    {
                        await _taskService.AssignOrRefreshTasksAsync(user.Id, activeAppTasks);
                        _logger.LogInformation("Successfully initiated assignment/refresh of tasks for user {UserId} on login.", user.Id);
                    }
                    else
                    {
                        _logger.LogInformation("No active AppTask definitions found. Skipping task assignment/refresh for user {UserId} on login.", user.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error assigning/refreshing tasks for user {UserId} on login. This will not fail the login.", user.Id);
                }

                var roles = await _userManager.GetRolesAsync(user);
                return new LoginResponseDTO
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken.Token,
                    UserId = user.Id,
                    FullName = user.FullName,
                    UserName = user.UserName ?? "",
                    Email = user.Email ?? "",
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    Roles = roles
                };
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning("Login failed for User {UserId}. EventType: {EventType}, FailureReason: {FailureReason}, LogCategory: {LogCategory}", user.Id, "LoginFailure", "AccountLockedOutTooManyAttempts", "SecurityAudit");
                throw new UnauthorizedAccessException("Hesabınız çok fazla başarısız giriş denemesi nedeniyle kilitlenmiştir.");
            }
            _logger.LogWarning("Login failed for User {UserId}. EventType: {EventType}, FailureReason: {FailureReason}, LogCategory: {LogCategory}", user.Id, "LoginFailure", "InvalidCredentials", "SecurityAudit");
            throw new UnauthorizedAccessException("Geçersiz e-posta/kullanıcı adı veya şifre.");
        }

        public async Task<IdentityResult> ConfirmEmailAsync(string userId, string token)
        {
            _logger.LogDebug("Attempting email confirmation for UserId {UserId}. TokenProvided: {TokenProvided}. EventType: {EventType}, LogCategory: {LogCategory}", userId, !string.IsNullOrEmpty(token), "EmailConfirmationAttempt", "SecurityAudit");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Email confirmation failed for UserId {UserId}. EventType: {EventType}, FailureReason: {FailureReason}, LogCategory: {LogCategory}", userId, "EmailConfirmationFailure", "UserNotFound", "SecurityAudit");
                return IdentityResult.Failed(new IdentityError { Description = "Email confirmation failed: User not found." });
            }

            if (user.EmailConfirmed)
            {
                _logger.LogInformation("Email already confirmed for UserId {UserId}. EventType: {EventType}, LogCategory: {LogCategory}", userId, "EmailAlreadyConfirmed", "SecurityAudit");
                return IdentityResult.Failed(new IdentityError { Description = "Email already confirmed." });
            }

            var decodedToken = WebUtility.UrlDecode(token);
            decodedToken = decodedToken.Replace(' ', '+');
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Decoded email confirmation token for UserId {UserId}: {DecodedToken}. EventType: {EventType}, LogCategory: {LogCategory}", userId, decodedToken, "EmailConfirmationTokenDecoded", "SecurityAudit");
            }

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => new { e.Code, e.Description }).ToList();
                _logger.LogWarning("Email confirmation failed for UserId {UserId}. Errors: {@Errors}. EventType: {EventType}, FailureReason: {FailureReason}, LogCategory: {LogCategory}", userId, errors, "EmailConfirmationFailure", errors.FirstOrDefault()?.Description ?? "UnknownError", "SecurityAudit");
            }
            else
            {
                _logger.LogInformation("Email confirmed successfully for UserId {UserId}. EventType: {EventType}, LogCategory: {LogCategory}", userId, "EmailConfirmationSuccess", "SecurityAudit");
            }

            return result;
        }

        public async Task SendPasswordResetAsync(ForgotPasswordDTO dto)
        {
            _logger.LogDebug("Password reset request for Email {Email}. EventType: {EventType}, LogCategory: {LogCategory}", dto.Email, "PasswordResetAttempt", "SecurityAudit");
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                _logger.LogWarning("Password reset request: User not found for Email {Email}. No email will be sent. EventType: {EventType}, FailureReason: {FailureReason}, LogCategory: {LogCategory}", dto.Email, "PasswordResetFailure", "UserNotFound", "SecurityAudit");
                return;
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebUtility.UrlEncode(token);
            var baseUrl = _config["AppBaseUrl"];
            if (string.IsNullOrEmpty(baseUrl))
            {
                _logger.LogError("AppBaseUrl is not configured. Cannot create password reset link for User {UserId}.", user.Id);
            }
            var resetLink = $"{baseUrl?.TrimEnd('/')}/reset-password?userId={user.Id}&token={encodedToken}";
            await _emailSender.SendEmailAsync(user.Email ?? "", "Reset Your Password", $"Please reset your password by clicking this link: {resetLink}");
            _logger.LogInformation("Password reset email sent to {Email} for User {UserId}. EventType: {EventType}, LogCategory: {LogCategory}", user.Email, user.Id, "PasswordResetEmailSent", "SecurityAudit");
        }

        public async Task<IdentityResult> ResetPasswordAsync(ResetPasswordDTO dto)
        {
            _logger.LogDebug("Password reset attempt for UserId {UserId}. TokenProvided: {TokenProvided}. EventType: {EventType}, LogCategory: {LogCategory}", dto.UserId, !string.IsNullOrEmpty(dto.Token), "PasswordResetAttempt", "SecurityAudit");
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null)
            {
                _logger.LogWarning("Password reset failed for UserId {UserId}. EventType: {EventType}, FailureReason: {FailureReason}, LogCategory: {LogCategory}", dto.UserId, "PasswordResetFailure", "UserNotFound", "SecurityAudit");
                return IdentityResult.Failed(new IdentityError { Description = "Invalid user." });
            }

            var decodedToken = WebUtility.UrlDecode(dto.Token);
            decodedToken = decodedToken.Replace(' ', '+');
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Decoded password reset token for UserId {UserId}: {DecodedToken}. EventType: {EventType}, LogCategory: {LogCategory}", dto.UserId, decodedToken, "PasswordResetTokenDecoded", "SecurityAudit");
            }

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, dto.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("Password reset successful for UserId {UserId}. Security stamp updated. EventType: {EventType}, LogCategory: {LogCategory}", user.Id, "PasswordResetSuccess", "SecurityAudit");
                await _userManager.UpdateSecurityStampAsync(user);
            }
            else
            {
                var errors = result.Errors.Select(e => new { e.Code, e.Description }).ToList();
                _logger.LogWarning("Password reset failed for UserId {UserId}. Errors: {@Errors}. EventType: {EventType}, FailureReason: {FailureReason}, LogCategory: {LogCategory}", dto.UserId, errors, "PasswordResetFailure", errors.FirstOrDefault()?.Description ?? "UnknownError", "SecurityAudit");
            }
            return result;
        }

        public async Task<IdentityResult> ChangePasswordAsync(ChangePasswordDTO dto, string userId)
        {
            _logger.LogDebug("Change password attempt for UserId {UserId}. EventType: {EventType}, LogCategory: {LogCategory}", userId, "PasswordChangeAttempt", "SecurityAudit");
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Change password failed for UserId {UserId}. EventType: {EventType}, FailureReason: {FailureReason}, LogCategory: {LogCategory}", userId, "PasswordChangeFailure", "UserNotFound", "SecurityAudit");
                return IdentityResult.Failed(new IdentityError { Description = "Invalid user." });
            }

            if (await _userManager.CheckPasswordAsync(user, dto.NewPassword))
            {
                _logger.LogWarning("Change password failed for UserId {UserId}. EventType: {EventType}, FailureReason: {FailureReason}, LogCategory: {LogCategory}", userId, "PasswordChangeFailure", "NewPasswordSameAsOld", "SecurityAudit");
                return IdentityResult.Failed(new IdentityError { Description = "New password cannot be same as old one." });
            }

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

            if (result.Succeeded)
            {
                _logger.LogInformation("Password changed successfully for UserId {UserId}. Security stamp updated. EventType: {EventType}, LogCategory: {LogCategory}", userId, "PasswordChangeSuccess", "SecurityAudit");
                await _userManager.UpdateSecurityStampAsync(user);
            }
            else
            {
                var errors = result.Errors.Select(e => new { e.Code, e.Description }).ToList();
                _logger.LogWarning("Change password failed for UserId {UserId}. Errors: {@Errors}. EventType: {EventType}, FailureReason: {FailureReason}, LogCategory: {LogCategory}", userId, errors, "PasswordChangeFailure", errors.FirstOrDefault()?.Description ?? "UnknownError", "SecurityAudit");
            }
            return result;
        }

        public async Task<LoginResponseDTO> RefreshTokenAsync(RefreshTokenRequestDTO requestDto)
        {
            _logger.LogDebug("Refresh token attempt. RefreshTokenProvided: {RefreshTokenProvided}. EventType: {EventType}, LogCategory: {LogCategory}", !string.IsNullOrEmpty(requestDto.RefreshToken), "TokenRefreshAttempt", "SecurityAudit");
            var storedToken = await _context.UserRefreshTokens
                                        .Include(rt => rt.User)
                                        .FirstOrDefaultAsync(rt => rt.Token == requestDto.RefreshToken);

            if (storedToken == null)
            {
                _logger.LogWarning("Refresh token failed. EventType: {EventType}, FailureReason: {FailureReason}, LogCategory: {LogCategory}", "TokenRefreshFailure", "RefreshTokenNotFound", "SecurityAudit");
                throw new SecurityTokenException("Geçersiz refresh token.");
            }
            _logger.LogDebug("Refresh token found for UserId {UserId}. TokenId: {TokenId}. EventType: {EventType}, LogCategory: {LogCategory}", storedToken.UserId, storedToken.Id, "RefreshTokenFound", "SecurityAudit");


            if (storedToken.IsRevoked)
            {
                _logger.LogWarning("Refresh token failed for UserId {UserId}, TokenId {TokenId}. EventType: {EventType}, FailureReason: {FailureReason}, LogCategory: {LogCategory}", storedToken.UserId, storedToken.Id, "TokenRefreshFailure", "TokenRevoked", "SecurityAudit");
                throw new SecurityTokenException("Refresh token iptal edilmiş.");
            }

            if (storedToken.IsExpired)
            {
                _logger.LogWarning("Refresh token failed for UserId {UserId}, TokenId {TokenId}. ExpiresAt: {ExpiresAt}. EventType: {EventType}, FailureReason: {FailureReason}, LogCategory: {LogCategory}", storedToken.UserId, storedToken.Id, storedToken.Expires, "TokenRefreshFailure", "TokenExpired", "SecurityAudit");
                throw new SecurityTokenException("Refresh token süresi dolmuş.");
            }

            var user = storedToken.User;
            if (user == null)
            {
                _logger.LogError("Refresh token failed: User not found for an active, non-expired token. TokenId {TokenId}, Stored UserId {StoredUserId}. EventType: {EventType}, FailureReason: {FailureReason}, LogCategory: {LogCategory}", storedToken.Id, storedToken.UserId, "TokenRefreshFailure", "UserNotFoundForToken", "SecurityAudit");
                throw new SecurityTokenException("Refresh token ile ilişkili kullanıcı bulunamadı.");
            }
            _logger.LogDebug("User {UserId} associated with refresh token {TokenId} found. EventType: {EventType}, LogCategory: {LogCategory}", user.Id, storedToken.Id, "UserForRefreshTokenFound", "SecurityAudit");

            var newAccessToken = await GenerateJwt(user);
            var newRefreshToken = GenerateRefreshToken(user.Id);

            storedToken.Revoked = DateTime.UtcNow;
            _logger.LogInformation("Old refresh token {OldTokenId} for User {UserId} revoked. EventType: {EventType}, LogCategory: {LogCategory}", storedToken.Id, user.Id, "OldRefreshTokenRevoked", "SecurityAudit");

            await _context.SaveChangesAsync();
            _logger.LogInformation("New access token and refresh token {NewTokenId} generated and saved for User {UserId}. AccessTokenExpiry: {AccessTokenExpiry}. EventType: {EventType}, LogCategory: {LogCategory}", newRefreshToken.Id, user.Id, DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["JwtSettings:ExpiryMinutes"] ?? "60")), "TokenRefreshSuccess", "SecurityAudit");

            try
            {
                _logger.LogInformation("Attempting to assign/refresh tasks for user {UserId} on token refresh.", user.Id);
                var activeAppTasks = await _context.AppTasks.Where(t => t.IsActive).ToListAsync();
                if (activeAppTasks.Any())
                {
                    await _taskService.AssignOrRefreshTasksAsync(user.Id, activeAppTasks);
                    _logger.LogInformation("Successfully initiated assignment/refresh of tasks for user {UserId} on token refresh.", user.Id);
                }
                else
                {
                    _logger.LogInformation("No active AppTask definitions found. Skipping task assignment/refresh for user {UserId} on token refresh.", user.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning/refreshing tasks for user {UserId} on token refresh. This will not fail the token refresh.", user.Id);
            }

            var roles = await _userManager.GetRolesAsync(user);
            return new LoginResponseDTO
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                UserId = user.Id,
                FullName = user.FullName,
                UserName = user.UserName ?? "",
                Email = user.Email ?? "",
                ProfilePictureUrl = user.ProfilePictureUrl,
                Roles = roles
            };
        }

        public async Task RevokeTokenAsync(string token)
        {
            _logger.LogDebug("Attempting to revoke refresh token. RefreshTokenProvided: {RefreshTokenProvided}. EventType: {EventType}, LogCategory: {LogCategory}", !string.IsNullOrEmpty(token), "TokenRevocationAttempt", "SecurityAudit");
            var storedToken = await _context.UserRefreshTokens
                                       .FirstOrDefaultAsync(rt => rt.Token == token);

            if (storedToken != null && storedToken.IsActive)
            {
                storedToken.Revoked = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Refresh token {TokenId} for UserId {UserId} revoked successfully. EventType: {EventType}, LogCategory: {LogCategory}", storedToken.Id, storedToken.UserId, "TokenRevocationSuccess", "SecurityAudit");
            }
            else if (storedToken != null)
            {
                _logger.LogInformation("Refresh token {TokenId} for UserId {UserId} was already inactive (revoked or expired). No action taken. EventType: {EventType}, LogCategory: {LogCategory}", storedToken.Id, storedToken.UserId, "TokenRevocationNoAction", "SecurityAudit");
            }
            else
            {
                _logger.LogWarning("Attempt to revoke refresh token failed. EventType: {EventType}, FailureReason: {FailureReason}, LogCategory: {LogCategory}", "TokenRevocationFailure", "RefreshTokenNotFound", "SecurityAudit");
            }
        }

        private async Task<string> GenerateJwt(AppUser user)
        {
            var key = Encoding.ASCII.GetBytes(_config["AppSettings:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured"));
            var tokenHandler = new JwtSecurityTokenHandler();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.AuthenticationInstant, DateTime.UtcNow.ToString("o")),
                new Claim("SecurityStamp", user.SecurityStamp ?? "")
            };

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["JwtSettings:ExpiryMinutes"] ?? "60")),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _config["JwtSettings:Issuer"],
                Audience = _config["JwtSettings:Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private UserRefreshToken GenerateRefreshToken(string userId)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            string? ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
            string? deviceBrowserInfo = httpContext?.Request.Headers["User-Agent"].ToString();

            var refreshToken = new UserRefreshToken
            {
                UserId = userId,
                Token = GenerateRefreshTokenString(),
                Expires = DateTime.UtcNow.AddDays(Convert.ToDouble(_config["JwtSettings:RefreshTokenExpiryDays"] ?? "7")),
                Created = DateTime.UtcNow,
                IpAddress = ipAddress,
                DeviceBrowserInfo = deviceBrowserInfo,
                LastUsedDate = DateTime.UtcNow
            };

            _context.UserRefreshTokens.Add(refreshToken);
            return refreshToken;
        }

        private static string GenerateRefreshTokenString()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}