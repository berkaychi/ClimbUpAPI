using System.Security.Claims;
using ClimbUpAPI.Models.DTOs;
using ClimbUpAPI.Models.DTOs.Gamification;
using ClimbUpAPI.Models.DTOs.SessionDTOs;
using ClimbUpAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClimbUpAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FocusSessionController : ControllerBase
    {
        private readonly IFocusSessionService _focusSessionService;
        private readonly ILogger<FocusSessionController> _logger;
        public FocusSessionController(IFocusSessionService focusSessionService, ILogger<FocusSessionController> logger)
        {
            _focusSessionService = focusSessionService;
            _logger = logger;
        }

        private string CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException("User not authenticated");

        [HttpPost]
        [ProducesResponseType(typeof(GamifiedResponse<FocusSessionResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] CreateFocusSessionDto dto)
        {
            try
            {
                _logger.LogDebug("Attempting to create focus session for User {UserId} with DTO: {@CreateFocusSessionDto}", CurrentUserId, dto);
                var gamifiedResponse = await _focusSessionService.CreateFocusSessionAsync(dto, CurrentUserId);
                _logger.LogInformation("Successfully created focus session {FocusSessionId} for User {UserId}", gamifiedResponse.Data.Id, CurrentUserId);
                return CreatedAtAction(nameof(GetById), new { id = gamifiedResponse.Data.Id }, gamifiedResponse);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Argument error while User {UserId} creating focus session. DTO: {@CreateFocusSessionDto}", CurrentUserId, dto);
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt by User {UserId} while creating focus session. DTO: {@CreateFocusSessionDto}", CurrentUserId, dto);
                return Unauthorized(new { message = "Bu işlem için yetkiniz yok." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} creating focus session. DTO: {@CreateFocusSessionDto}", CurrentUserId, dto);
                return StatusCode(500, new { message = "Oturum oluşturulurken sunucu hatası oluştu. Lütfen daha sonra tekrar deneyin." });
            }
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<FocusSessionResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                _logger.LogDebug("Attempting to get all focus sessions for User {UserId}", CurrentUserId);
                var sessions = await _focusSessionService.GetUserSessionsAsync(CurrentUserId);
                _logger.LogInformation("Successfully retrieved {SessionCount} focus sessions for User {UserId}", sessions.Count, CurrentUserId);
                return Ok(sessions);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt by User {UserId} while getting all focus sessions", CurrentUserId);
                return Unauthorized(new { message = "Bu işlem için yetkiniz yok." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} getting all focus sessions", CurrentUserId);
                return StatusCode(500, new { message = "Oturumlar alınırken sunucu hatası oluştu. Lütfen daha sonra tekrar deneyin." });
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(FocusSessionResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                _logger.LogDebug("Attempting to get focus session {FocusSessionId} for User {UserId}", id, CurrentUserId);
                var session = await _focusSessionService.GetByIdAsync(id, CurrentUserId);
                if (session == null)
                {
                    _logger.LogWarning("Focus session {FocusSessionId} not found for User {UserId}", id, CurrentUserId);
                    return NotFound(new { message = "Belirtilen odak oturumu bulunamadı." });
                }
                _logger.LogInformation("Successfully retrieved focus session {FocusSessionId} for User {UserId}. StartTime: {StartTime}, EndTime: {EndTime}, SessionTypeId: {SessionTypeId}", id, CurrentUserId, session.StartTime, session.EndTime, session.SessionTypeId);
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Full FocusSession data for {FocusSessionId} for User {UserId}: {@FocusSession}", id, CurrentUserId, session);
                }
                return Ok(session);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt by User {UserId} while getting focus session {FocusSessionId}", CurrentUserId, id);
                return Unauthorized(new { message = "Bu işlem için yetkiniz yok." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} getting focus session {FocusSessionId}", CurrentUserId, id);
                return StatusCode(500, new { message = "Oturum alınırken sunucu hatası oluştu. Lütfen daha sonra tekrar deneyin." });
            }
        }


        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateFocusSessionDto dto)
        {
            try
            {
                _logger.LogDebug("Attempting to update focus session {FocusSessionId} for User {UserId} with DTO: {@UpdateFocusSessionDto}", id, CurrentUserId, dto);
                await _focusSessionService.UpdateAsync(id, dto, CurrentUserId);
                _logger.LogInformation("Successfully updated focus session {FocusSessionId} for User {UserId}", id, CurrentUserId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Focus session {FocusSessionId} not found for update by User {UserId}. DTO: {@UpdateFocusSessionDto}", id, CurrentUserId, dto);
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Argument error while User {UserId} updating focus session {FocusSessionId}. DTO: {@UpdateFocusSessionDto}", CurrentUserId, id, dto);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while User {UserId} updating focus session {FocusSessionId}. DTO: {@UpdateFocusSessionDto}", CurrentUserId, id, dto);
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt by User {UserId} while updating focus session {FocusSessionId}. DTO: {@UpdateFocusSessionDto}", CurrentUserId, id, dto);
                return Unauthorized(new { message = "Bu işlem için yetkiniz yok." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} updating focus session {FocusSessionId}. DTO: {@UpdateFocusSessionDto}", CurrentUserId, id, dto);
                return StatusCode(500, new { message = "Oturum güncellenirken sunucu hatası oluştu. Lütfen daha sonra tekrar deneyin." });
            }
        }

        [HttpPatch("{id}/status")]
        [ProducesResponseType(typeof(AwardedPointsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateFocusSessionStatusDto dto)
        {
            try
            {
                _logger.LogDebug("Attempting to update status for focus session {FocusSessionId} for User {UserId} with DTO: {@UpdateFocusSessionStatusDto}", id, CurrentUserId, dto);
                var awardedPoints = await _focusSessionService.UpdateStatusAsync(id, dto, CurrentUserId);
                _logger.LogInformation("Successfully updated status for focus session {FocusSessionId} for User {UserId} to {Status}", id, CurrentUserId, dto.Status);

                if (awardedPoints != null)
                {
                    return Ok(awardedPoints);
                }
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Focus session {FocusSessionId} not found for status update by User {UserId}. DTO: {@UpdateFocusSessionStatusDto}", id, CurrentUserId, dto);
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Argument error while User {UserId} updating status for focus session {FocusSessionId}. DTO: {@UpdateFocusSessionStatusDto}", CurrentUserId, id, dto);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while User {UserId} updating status for focus session {FocusSessionId}. DTO: {@UpdateFocusSessionStatusDto}", CurrentUserId, id, dto);
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt by User {UserId} while updating status for focus session {FocusSessionId}. DTO: {@UpdateFocusSessionStatusDto}", CurrentUserId, id, dto);
                return Unauthorized(new { message = "Bu işlem için yetkiniz yok." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} updating status for focus session {FocusSessionId}. DTO: {@UpdateFocusSessionStatusDto}", CurrentUserId, id, dto);
                return StatusCode(500, new { message = "Oturum durumu güncellenirken sunucu hatası oluştu. Lütfen daha sonra tekrar deneyin." });
            }
        }

        [HttpGet("ongoing")]
        [ProducesResponseType(typeof(FocusSessionResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetOngoingSession()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims while trying to get ongoing session.");
                    return Unauthorized(new { message = "User not authenticated." });
                }

                _logger.LogDebug("Attempting to get ongoing focus session for User {UserId}", userId);
                var ongoingSession = await _focusSessionService.GetOngoingSessionAsync(userId);

                if (ongoingSession == null)
                {
                    _logger.LogInformation("No ongoing focus session found for User {UserId}", userId);
                    return NotFound(new { message = "Devam eden bir odak oturumu bulunamadı." });
                }

                _logger.LogInformation("Successfully retrieved ongoing focus session for User {UserId}", userId);
                return Ok(ongoingSession);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt while getting ongoing focus session. UserId could not be determined or service indicated unauthorized.");
                return Unauthorized(new { message = "Bu işlem için yetkiniz yok." });
            }
            catch (Exception ex)
            {
                var userIdForLogging = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
                _logger.LogError(ex, "Error while User {UserId} getting ongoing focus session", userIdForLogging);
                return StatusCode(500, new { message = "Devam eden oturum alınırken sunucu hatası oluştu. Lütfen daha sonra tekrar deneyin." });
            }
        }
        [HttpPost("{sessionId}/transition-state")]
        [ProducesResponseType(typeof(GamifiedResponse<FocusSessionResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> TransitionState(int sessionId)
        {
            try
            {
                _logger.LogDebug("Attempting to transition state for focus session {FocusSessionId} for User {UserId}", sessionId, CurrentUserId);
                var gamifiedResponse = await _focusSessionService.TransitionSessionStateAsync(sessionId, CurrentUserId);
                _logger.LogInformation("Successfully transitioned state for focus session {FocusSessionId} for User {UserId}. New Status: {NewStatus}",
                    gamifiedResponse.Data.Id, CurrentUserId, gamifiedResponse.Data.Status);
                return Ok(gamifiedResponse);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Focus session {FocusSessionId} not found for phase advancement by User {UserId}", sessionId, CurrentUserId);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while User {UserId} advancing phase for focus session {FocusSessionId}", CurrentUserId, sessionId);
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt by User {UserId} while advancing phase for focus session {FocusSessionId}", CurrentUserId, sessionId);
                return Unauthorized(new { message = "Bu işlem için yetkiniz yok." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} advancing phase for focus session {FocusSessionId}", CurrentUserId, sessionId);
                return StatusCode(500, new { message = "Oturum fazı ilerletilirken sunucu hatası oluştu. Lütfen daha sonra tekrar deneyin." });
            }
        }
    }
}
