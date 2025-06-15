using ClimbUpAPI.Models.DTOs.Admin.SessionTypeDTOs;
using ClimbUpAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Security.Claims;

namespace ClimbUpAPI.Controllers.Admin
{
    public class AdminSessionTypeController : AdminBaseController
    {
        private readonly ISessionTypeService _sessionTypeService;
        private readonly ILogger<AdminSessionTypeController> _logger;

        public AdminSessionTypeController(ISessionTypeService sessionTypeService, ILogger<AdminSessionTypeController> logger)
        {
            _sessionTypeService = sessionTypeService;
            _logger = logger;
        }

        private string? AdminUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // POST api/admin/sessiontype
        [HttpPost]
        public async Task<IActionResult> CreateSessionType([FromBody] AdminCreateSessionTypeDto dto)
        {
            _logger.LogInformation("Admin {AdminUserId} attempting to create session type with DTO: {@AdminCreateSessionTypeDto}", AdminUserId ?? "UnknownAdmin", dto);
            try
            {
                var createdSessionType = await _sessionTypeService.CreateAdminSessionTypeAsync(dto);
                _logger.LogInformation("Admin {AdminUserId} successfully created session type {SessionTypeId}. Name: {SessionTypeName}", AdminUserId ?? "UnknownAdmin", createdSessionType.Id, createdSessionType.Name);
                return CreatedAtAction(nameof(GetSessionTypeById), new { id = createdSessionType.Id }, createdSessionType);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Admin {AdminUserId} failed to create session type. DTO: {@AdminCreateSessionTypeDto}. Reason: {ErrorMessage}", AdminUserId ?? "UnknownAdmin", dto, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin {AdminUserId} - Error creating session type. DTO: {@AdminCreateSessionTypeDto}", AdminUserId ?? "UnknownAdmin", dto);
                return StatusCode(500, new { message = "An unexpected error occurred while creating the session type." });
            }
        }

        // GET api/admin/sessiontype
        [HttpGet]
        public async Task<IActionResult> GetAllSessionTypes([FromQuery] string scope = "all", [FromQuery] bool includeArchived = false)
        {
            _logger.LogInformation("Admin {AdminUserId} attempting to get all session types. Scope: {Scope}, IncludeArchived: {IncludeArchived}", AdminUserId ?? "UnknownAdmin", scope, includeArchived);

            string lowerScope = scope.ToLowerInvariant();
            if (lowerScope != "all" && lowerScope != "system")
            {
                _logger.LogWarning("Admin {AdminUserId} provided invalid scope parameter: {Scope}. Defaulting to 'all'.", AdminUserId ?? "UnknownAdmin", scope);
                return BadRequest(new { message = "Invalid scope parameter. Allowed values are 'all' or 'system'." });
            }

            try
            {
                var sessionTypes = await _sessionTypeService.GetAllSessionTypesForAdminAsync(lowerScope, includeArchived);
                _logger.LogInformation("Admin {AdminUserId} successfully retrieved {SessionTypeCount} session types for Scope: {Scope}, IncludeArchived: {IncludeArchived}.", AdminUserId ?? "UnknownAdmin", sessionTypes.Count, lowerScope, includeArchived);
                return Ok(sessionTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin {AdminUserId} - Error retrieving all session types. Scope: {Scope}, IncludeArchived: {IncludeArchived}", AdminUserId ?? "UnknownAdmin", lowerScope, includeArchived);
                return StatusCode(500, new { message = "An unexpected error occurred while retrieving session types." });
            }
        }

        // GET api/admin/sessiontype/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSessionTypeById(int id)
        {
            _logger.LogInformation("Admin {AdminUserId} attempting to get session type {SessionTypeId}.", AdminUserId ?? "UnknownAdmin", id);
            try
            {
                var sessionType = await _sessionTypeService.GetSessionTypeByIdForAdminAsync(id);
                if (sessionType == null)
                {
                    _logger.LogWarning("Admin {AdminUserId} - Session type {SessionTypeId} not found.", AdminUserId ?? "UnknownAdmin", id);
                    return NotFound(new { message = "Session type not found." });
                }
                _logger.LogInformation("Admin {AdminUserId} successfully retrieved session type {SessionTypeId}. Name: {SessionTypeName}", AdminUserId ?? "UnknownAdmin", id, sessionType.Name);
                return Ok(sessionType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin {AdminUserId} - Error retrieving session type {SessionTypeId}.", AdminUserId ?? "UnknownAdmin", id);
                return StatusCode(500, new { message = "An unexpected error occurred while retrieving the session type." });
            }
        }

        // PUT api/admin/sessiontype/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSessionType(int id, [FromBody] AdminUpdateSessionTypeDto dto)
        {
            _logger.LogInformation("Admin {AdminUserId} attempting to update session type {SessionTypeId} with DTO: {@AdminUpdateSessionTypeDto}", AdminUserId ?? "UnknownAdmin", id, dto);
            try
            {
                var updatedSessionType = await _sessionTypeService.UpdateSessionTypeForAdminAsync(id, dto);
                _logger.LogInformation("Admin {AdminUserId} successfully updated session type {SessionTypeId}. Name: {SessionTypeName}", AdminUserId ?? "UnknownAdmin", id, updatedSessionType.Name);
                return Ok(updatedSessionType);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Admin {AdminUserId} - Session type {SessionTypeId} not found for update. DTO: {@AdminUpdateSessionTypeDto}", AdminUserId ?? "UnknownAdmin", id, dto);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Admin {AdminUserId} - Invalid operation while updating session type {SessionTypeId}. DTO: {@AdminUpdateSessionTypeDto}. Reason: {ErrorMessage}", AdminUserId ?? "UnknownAdmin", id, dto, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin {AdminUserId} - Error updating session type {SessionTypeId}. DTO: {@AdminUpdateSessionTypeDto}", AdminUserId ?? "UnknownAdmin", id, dto);
                return StatusCode(500, new { message = "An unexpected error occurred while updating the session type." });
            }
        }

        // PATCH api/admin/sessiontype/{id}/archive
        [HttpPatch("{id}/archive")]
        public async Task<IActionResult> ArchiveSessionType(int id)
        {
            _logger.LogInformation("Admin {AdminUserId} attempting to archive session type {SessionTypeId}.", AdminUserId ?? "UnknownAdmin", id);
            try
            {
                await _sessionTypeService.ArchiveSessionTypeAsync(id);
                _logger.LogInformation("Admin {AdminUserId} successfully archived session type {SessionTypeId}.", AdminUserId ?? "UnknownAdmin", id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Admin {AdminUserId} - Session type {SessionTypeId} not found for archival. Reason: {ErrorMessage}", AdminUserId ?? "UnknownAdmin", id, ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Admin {AdminUserId} - Invalid operation while archiving session type {SessionTypeId}. Reason: {ErrorMessage}", AdminUserId ?? "UnknownAdmin", id, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin {AdminUserId} - Error archiving session type {SessionTypeId}.", AdminUserId ?? "UnknownAdmin", id);
                return StatusCode(500, new { message = "An unexpected error occurred while archiving the session type." });
            }
        }

        // PATCH api/admin/sessiontype/{id}/unarchive
        [HttpPatch("{id}/unarchive")]
        public async Task<IActionResult> UnarchiveSessionType(int id)
        {
            _logger.LogInformation("Admin {AdminUserId} attempting to unarchive session type {SessionTypeId}.", AdminUserId ?? "UnknownAdmin", id);
            try
            {
                await _sessionTypeService.UnarchiveSessionTypeAsync(id);
                _logger.LogInformation("Admin {AdminUserId} successfully unarchived session type {SessionTypeId}.", AdminUserId ?? "UnknownAdmin", id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Admin {AdminUserId} - Session type {SessionTypeId} not found for unarchival. Reason: {ErrorMessage}", AdminUserId ?? "UnknownAdmin", id, ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Admin {AdminUserId} - Invalid operation while unarchiving session type {SessionTypeId}. Reason: {ErrorMessage}", AdminUserId ?? "UnknownAdmin", id, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin {AdminUserId} - Error unarchiving session type {SessionTypeId}.", AdminUserId ?? "UnknownAdmin", id);
                return StatusCode(500, new { message = "An unexpected error occurred while unarchiving the session type." });
            }
        }

        // DELETE api/admin/sessiontype/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSessionType(int id)
        {
            _logger.LogInformation("Admin {AdminUserId} attempting to delete session type {SessionTypeId}.", AdminUserId ?? "UnknownAdmin", id);
            try
            {
                await _sessionTypeService.DeleteSessionTypeForAdminAsync(id);
                _logger.LogInformation("Admin {AdminUserId} successfully deleted session type {SessionTypeId}.", AdminUserId ?? "UnknownAdmin", id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Admin {AdminUserId} - Session type {SessionTypeId} not found for deletion. Reason: {ErrorMessage}", AdminUserId ?? "UnknownAdmin", id, ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Admin {AdminUserId} - Invalid operation while deleting session type {SessionTypeId}. Reason: {ErrorMessage}", AdminUserId ?? "UnknownAdmin", id, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin {AdminUserId} - Error deleting session type {SessionTypeId}.", AdminUserId ?? "UnknownAdmin", id);
                return StatusCode(500, new { message = "An unexpected error occurred while deleting the session type." });
            }
        }
    }
}