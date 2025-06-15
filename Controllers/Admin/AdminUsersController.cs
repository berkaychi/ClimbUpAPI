using ClimbUpAPI.Models.DTOs.Admin;
using ClimbUpAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ClimbUpAPI.Controllers.Admin
{
    public class AdminUsersController : AdminBaseController
    {
        private readonly IAdminUserService _adminUserService;
        private readonly ILogger<AdminUsersController> _logger;

        public AdminUsersController(IAdminUserService adminUserService, ILogger<AdminUsersController> logger)
        {
            _adminUserService = adminUserService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<AdminUserListDto>>> GetUsers()
        {
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogDebug("Admin User {AdminUserId} attempting to get all users for admin panel.", adminUserId ?? "UnknownAdmin");
            try
            {
                var users = await _adminUserService.GetUsersForAdminAsync();
                _logger.LogInformation("Admin User {AdminUserId} successfully retrieved {UserCount} users for admin panel.", adminUserId ?? "UnknownAdmin", users.Count);
                if (_logger.IsEnabled(LogLevel.Debug) && users.Any())
                {
                    _logger.LogDebug("Admin User {AdminUserId} retrieved users summary: {@UserListSummary}", adminUserId ?? "UnknownAdmin", users.Select(u => new { u.Id, u.UserName, u.Email }).ToList());
                }
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users for admin panel. AdminUserId: {AdminUserId}", adminUserId ?? "UnknownAdmin");
                return StatusCode(500, "An unexpected error occurred while retrieving users.");
            }
        }

        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogDebug("Admin User {AdminUserId} attempting to delete User {TargetUserId}.", adminUserId ?? "UnknownAdmin", userId);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Admin User {AdminUserId} attempted to delete user with empty UserId. EventType: {EventType}, LogCategory: {LogCategory}", adminUserId ?? "UnknownAdmin", "AdminUserDeletionFailure", "SecurityAudit");
                return BadRequest("User ID cannot be empty.");
            }

            if (userId == adminUserId)
            {
                _logger.LogWarning("Admin User {AdminUserId} attempted to delete their own account. EventType: {EventType}, LogCategory: {LogCategory}", adminUserId, "AdminUserDeletionFailure", "SecurityAudit");
                return BadRequest("Administrators cannot delete their own account.");
            }

            try
            {
                var result = await _adminUserService.DeleteUserByAdminAsync(userId);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Admin User {AdminUserId} successfully deleted User {TargetUserId}. EventType: {EventType}, LogCategory: {LogCategory}", adminUserId ?? "UnknownAdmin", userId, "AdminUserDeletionSuccess", "SecurityAudit");
                    return NoContent();
                }

                var errors = result.Errors.Select(e => e.Description).ToList();
                if (result.Errors.Any(e => e.Description.Contains("User not found")))
                {
                    _logger.LogWarning("Admin User {AdminUserId} failed to delete User {TargetUserId}. Reason: User not found. EventType: {EventType}, LogCategory: {LogCategory}", adminUserId ?? "UnknownAdmin", userId, "AdminUserDeletionFailure", "SecurityAudit");
                    return NotFound($"User with ID {userId} not found.");
                }

                _logger.LogWarning("Admin User {AdminUserId} failed to delete User {TargetUserId}. Reasons: {@Errors}. EventType: {EventType}, LogCategory: {LogCategory}", adminUserId ?? "UnknownAdmin", userId, errors, "AdminUserDeletionFailure", "SecurityAudit");
                return BadRequest("Failed to delete user.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during deletion of User {TargetUserId} by Admin {AdminUserId}. EventType: {EventType}, LogCategory: {LogCategory}", userId, adminUserId ?? "UnknownAdmin", "AdminUserDeletionError", "SecurityAudit");
                return StatusCode(500, "An unexpected error occurred while deleting the user.");
            }
        }

        [HttpPost("{userId}/roles")]
        public async Task<IActionResult> AssignRole(string userId, [FromBody] AssignRoleDto assignRoleDto)
        {
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogDebug("Admin User {AdminUserId} attempting to assign role {RoleName} to User {TargetUserId}.", adminUserId ?? "UnknownAdmin", assignRoleDto.RoleName, userId);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Admin User {AdminUserId} attempted to assign role {RoleName} with empty TargetUserId. EventType: {EventType}, LogCategory: {LogCategory}", adminUserId ?? "UnknownAdmin", assignRoleDto.RoleName, "AdminRoleAssignmentFailure", "SecurityAudit");
                return BadRequest("User ID cannot be empty.");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Admin User {AdminUserId} attempt to assign role {RoleName} to User {TargetUserId} failed due to invalid model state: {@ModelStateErrors}. EventType: {EventType}, LogCategory: {LogCategory}",
                    adminUserId ?? "UnknownAdmin", assignRoleDto.RoleName, userId, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage), "AdminRoleAssignmentFailure", "SecurityAudit");
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _adminUserService.AssignRoleAsync(userId, assignRoleDto.RoleName);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Admin User {AdminUserId} successfully assigned role {RoleName} to User {TargetUserId}. EventType: {EventType}, LogCategory: {LogCategory}", adminUserId ?? "UnknownAdmin", assignRoleDto.RoleName, userId, "AdminRoleAssignmentSuccess", "SecurityAudit");
                    return Ok($"Role '{assignRoleDto.RoleName}' assigned successfully to user {userId}.");
                }

                var errors = result.Errors.Select(e => e.Description).ToList();
                if (result.Errors.Any(e => e.Description.Contains("not found")))
                {
                    _logger.LogWarning("Admin User {AdminUserId} failed to assign role {RoleName} to User {TargetUserId}. Reason: {Reason}. EventType: {EventType}, LogCategory: {LogCategory}", adminUserId ?? "UnknownAdmin", assignRoleDto.RoleName, userId, errors.First(), "AdminRoleAssignmentFailure", "SecurityAudit");
                    return NotFound(errors.First());
                }
                if (result.Errors.Any(e => e.Description.Contains("already has the role")))
                {
                    _logger.LogWarning("Admin User {AdminUserId} failed to assign role {RoleName} to User {TargetUserId}. Reason: {Reason}. EventType: {EventType}, LogCategory: {LogCategory}", adminUserId ?? "UnknownAdmin", assignRoleDto.RoleName, userId, errors.First(), "AdminRoleAssignmentFailure", "SecurityAudit");
                    return BadRequest(errors.First());
                }

                _logger.LogWarning("Admin User {AdminUserId} failed to assign role {RoleName} to User {TargetUserId}. Reasons: {@Errors}. EventType: {EventType}, LogCategory: {LogCategory}", adminUserId ?? "UnknownAdmin", assignRoleDto.RoleName, userId, errors, "AdminRoleAssignmentFailure", "SecurityAudit");
                return BadRequest("Failed to assign role.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during assignment of role {RoleName} to User {TargetUserId} by Admin {AdminUserId}. EventType: {EventType}, LogCategory: {LogCategory}", assignRoleDto.RoleName, userId, adminUserId ?? "UnknownAdmin", "AdminRoleAssignmentError", "SecurityAudit");
                return StatusCode(500, "An unexpected error occurred while assigning the role.");
            }
        }

        [HttpDelete("{userId}/roles/{roleName}")]
        public async Task<IActionResult> RemoveRole(string userId, string roleName)
        {
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogDebug("Admin User {AdminUserId} attempting to remove role {RoleName} from User {TargetUserId}.", adminUserId ?? "UnknownAdmin", roleName, userId);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Admin User {AdminUserId} attempted to remove role {RoleName} with empty TargetUserId. EventType: {EventType}, LogCategory: {LogCategory}", adminUserId ?? "UnknownAdmin", roleName, "AdminRoleRemovalFailure", "SecurityAudit");
                return BadRequest("User ID cannot be empty.");
            }
            if (string.IsNullOrEmpty(roleName))
            {
                _logger.LogWarning("Admin User {AdminUserId} attempted to remove role from User {TargetUserId} with empty RoleName. EventType: {EventType}, LogCategory: {LogCategory}", adminUserId ?? "UnknownAdmin", userId, "AdminRoleRemovalFailure", "SecurityAudit");
                return BadRequest("Role name cannot be empty.");
            }

            if (roleName != "Admin" && roleName != "User")
            {
                _logger.LogWarning("Admin User {AdminUserId} attempted to remove invalid role {RoleName} from User {TargetUserId}. EventType: {EventType}, LogCategory: {LogCategory}", adminUserId ?? "UnknownAdmin", roleName, userId, "AdminRoleRemovalFailure", "SecurityAudit");
                return BadRequest("Invalid role name. Must be 'Admin' or 'User'.");
            }

            try
            {
                var result = await _adminUserService.RemoveRoleAsync(userId, roleName);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Admin User {AdminUserId} successfully removed role {RoleName} from User {TargetUserId}. EventType: {EventType}, LogCategory: {LogCategory}", adminUserId ?? "UnknownAdmin", roleName, userId, "AdminRoleRemovalSuccess", "SecurityAudit");
                    return NoContent();
                }

                var errors = result.Errors.Select(e => e.Description).ToList();
                if (result.Errors.Any(e => e.Description.Contains("not found")))
                {
                    _logger.LogWarning("Admin User {AdminUserId} failed to remove role {RoleName} from User {TargetUserId}. Reason: {Reason}. EventType: {EventType}, LogCategory: {LogCategory}", adminUserId ?? "UnknownAdmin", roleName, userId, errors.First(), "AdminRoleRemovalFailure", "SecurityAudit");
                    return NotFound(errors.First());
                }
                if (result.Errors.Any(e => e.Description.Contains("does not have the role")))
                {
                    _logger.LogWarning("Admin User {AdminUserId} failed to remove role {RoleName} from User {TargetUserId}. Reason: {Reason}. EventType: {EventType}, LogCategory: {LogCategory}", adminUserId ?? "UnknownAdmin", roleName, userId, errors.First(), "AdminRoleRemovalFailure", "SecurityAudit");
                    return BadRequest(errors.First());
                }

                _logger.LogWarning("Admin User {AdminUserId} failed to remove role {RoleName} from User {TargetUserId}. Reasons: {@Errors}. EventType: {EventType}, LogCategory: {LogCategory}", adminUserId ?? "UnknownAdmin", roleName, userId, errors, "AdminRoleRemovalFailure", "SecurityAudit");
                return BadRequest("Failed to remove role.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during removal of role {RoleName} from User {TargetUserId} by Admin {AdminUserId}. EventType: {EventType}, LogCategory: {LogCategory}", roleName, userId, adminUserId ?? "UnknownAdmin", "AdminRoleRemovalError", "SecurityAudit");
                return StatusCode(500, "An unexpected error occurred while removing the role.");
            }
        }
    }
}