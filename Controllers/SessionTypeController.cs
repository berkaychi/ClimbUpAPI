using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClimbUpAPI.Services;
using System.Security.Claims;
using ClimbUpAPI.Models.DTOs.SessionDTOs;
using SQLitePCL;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace ClimbUpAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SessionTypeController(ISessionTypeService sessionTypeService, ILogger<SessionTypeController> logger) : ControllerBase
    {
        private readonly ISessionTypeService _sessionTypeService = sessionTypeService;
        private readonly ILogger<SessionTypeController> _logger = logger;

        private string CurrentUserId =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Kullanıcı kimliği alınamadı.");

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogDebug("User {UserId} attempting to get all session types.", CurrentUserId);
            try
            {
                var types = await _sessionTypeService.GetAvailableTypesAsync(CurrentUserId);
                _logger.LogInformation("Successfully retrieved {SessionTypeCount} session types for User {UserId}.", types.Count(), CurrentUserId);
                return Ok(types);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt by User {UserId} while getting all session types.", CurrentUserId);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} getting all session types.", CurrentUserId);
                return StatusCode(500, new { message = "Oturum tipleri alınırken bir hata oluştu." });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogDebug("User {UserId} attempting to get session type {SessionTypeId}.", CurrentUserId, id);
            try
            {
                var sessionType = await _sessionTypeService.GetByIdAsync(id, CurrentUserId);
                if (sessionType == null)
                {
                    _logger.LogWarning("Session type {SessionTypeId} was not found (returned null) for User {UserId}.", id, CurrentUserId);
                    return NotFound(new { message = $"Oturum tipi {id} bulunamadı." });
                }
                _logger.LogInformation("Successfully retrieved session type {SessionTypeId} for User {UserId}. Name: {SessionTypeName}, WorkDuration: {WorkDuration}", id, CurrentUserId, sessionType.Name, sessionType.WorkDuration);
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Full SessionType data for {SessionTypeId} for User {UserId}: {@SessionType}", id, CurrentUserId, sessionType);
                }
                return Ok(sessionType);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Session type {SessionTypeId} not found for User {UserId}.", id, CurrentUserId);
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access by User {UserId} for session type {SessionTypeId}.", CurrentUserId, id);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} getting session type {SessionTypeId}.", CurrentUserId, id);
                return StatusCode(500, new { message = "Oturum tipi alınırken bir hata oluştu." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSessionTypeDto dto)
        {
            _logger.LogDebug("User {UserId} attempting to create session type with DTO: {@CreateSessionTypeDto}", CurrentUserId, dto);
            try
            {
                var id = await _sessionTypeService.CreateAsync(dto, CurrentUserId);
                _logger.LogInformation("User {UserId} successfully created session type {SessionTypeId}. Name: {SessionTypeName}", CurrentUserId, id, dto.Name);
                var createdSessionType = await _sessionTypeService.GetByIdAsync(id, CurrentUserId);
                if (createdSessionType == null)
                {
                    _logger.LogError("Failed to retrieve newly created session type {SessionTypeId} for user {UserId} immediately after creation.", id, CurrentUserId);
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Yeni oluşturulan oturum tipi doğrulaması sırasında bir hata oluştu." });
                }
                return CreatedAtAction(nameof(GetById), new { id = createdSessionType.Id }, createdSessionType);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while User {UserId} creating session type. DTO: {@CreateSessionTypeDto}", CurrentUserId, dto);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} creating session type. DTO: {@CreateSessionTypeDto}", CurrentUserId, dto);
                return StatusCode(500, new { message = "Oturum tipi oluşturulurken bir hata oluştu." });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSessionTypeDto dto)
        {
            _logger.LogDebug("User {UserId} attempting to update session type {SessionTypeId} with DTO: {@UpdateSessionTypeDto}", CurrentUserId, id, dto);
            try
            {
                var updatedSessionType = await _sessionTypeService.UpdateAsync(id, dto, CurrentUserId);

                if (updatedSessionType == null)
                {
                    _logger.LogWarning("Session type {SessionTypeId} updated, but GetByIdAsync returned null (possibly IsActive is false). User {UserId}.", id, CurrentUserId);
                    return NotFound(new { message = $"Oturum tipi {id} güncellendi ancak mevcut durumuyla alınamadı (muhtemelen aktif değil)." });
                }

                _logger.LogInformation("User {UserId} successfully updated session type {SessionTypeId}. Name: {SessionTypeName}", CurrentUserId, id, updatedSessionType.Name);
                return Ok(updatedSessionType);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Session type {SessionTypeId} not found for update by User {UserId}. DTO: {@UpdateSessionTypeDto}", id, CurrentUserId, dto);
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized update attempt by User {UserId} for session type {SessionTypeId}. DTO: {@UpdateSessionTypeDto}", CurrentUserId, id, dto);
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while User {UserId} updating session type {SessionTypeId}. DTO: {@UpdateSessionTypeDto}", CurrentUserId, id, dto);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} updating session type {SessionTypeId}. DTO: {@UpdateSessionTypeDto}", CurrentUserId, id, dto);
                return StatusCode(500, new { message = "Oturum tipi güncellenirken bir hata oluştu." });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogDebug("User {UserId} attempting to delete session type {SessionTypeId}.", CurrentUserId, id);
            try
            {
                await _sessionTypeService.DeleteAsync(id, CurrentUserId);
                _logger.LogInformation("User {UserId} successfully deleted session type {SessionTypeId}.", CurrentUserId, id);
                return Ok("Session type deleted successfully");
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Session type {SessionTypeId} not found for delete by User {UserId}.", id, CurrentUserId);
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized delete attempt by User {UserId} for session type {SessionTypeId}.", CurrentUserId, id);
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while User {UserId} deleting session type {SessionTypeId}.", CurrentUserId, id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} deleting session type {SessionTypeId}.", CurrentUserId, id);
                return StatusCode(500, new { message = "Oturum tipi silinirken bir hata oluştu." });
            }
        }
    }

}
