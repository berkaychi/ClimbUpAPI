using ClimbUpAPI.Models.DTOs.ToDoDTOs;
using ClimbUpAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ClimbUpAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ToDoController(IToDoService toDoService, ILogger<ToDoController> logger) : ControllerBase
    {
        private readonly IToDoService _toDoService = toDoService;
        private readonly ILogger<ToDoController> _logger = logger;

        private string GetCurrentUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("User ID not found in token");
            return userId;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateToDoItemDto dto)
        {
            string? userId = null;
            try
            {
                userId = GetCurrentUserId();
                _logger.LogDebug("User {UserId} attempting to create ToDoItem with DTO: {@CreateToDoItemDto}", userId, dto);
                var toDo = await _toDoService.CreateAsync(dto, userId);
                if (toDo == null)
                {
                    _logger.LogError("Failed to create ToDoItem or retrieve it after creation for User {UserId}. DTO: {@CreateToDoItemDto}", userId, dto);
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Görev oluşturulurken veya oluşturulduktan sonra getirilirken bir hata oluştu." });
                }
                _logger.LogInformation("User {UserId} successfully created ToDoItem {ToDoItemId}: '{ToDoTitle}'", userId, toDo.Id, toDo.Title);
                return CreatedAtAction(nameof(GetById), new { id = toDo.Id }, toDo);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Argument error while User {UserId} creating ToDoItem. DTO: {@CreateToDoItemDto}", userId ?? "Unknown", dto);
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt by User {UserId} while creating ToDoItem. DTO: {@CreateToDoItemDto}", userId ?? "Unknown", dto);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} creating ToDoItem. DTO: {@CreateToDoItemDto}", userId ?? "Unknown", dto);
                return StatusCode(500, new { message = "Görev oluşturulurken bir hata oluştu." });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            string? userId = null;
            try
            {
                userId = GetCurrentUserId();
                _logger.LogDebug("User {UserId} attempting to get ToDoItem {ToDoItemId}.", userId, id);
                var toDo = await _toDoService.GetByIdAsync(id, userId);
                if (toDo == null)
                {
                    _logger.LogWarning("ToDoItem {ToDoItemId} was not found (returned null) for User {UserId}.", id, userId);
                    return NotFound(new { message = $"Görev {id} bulunamadı." });
                }
                _logger.LogInformation("Successfully retrieved ToDoItem {ToDoItemId} for User {UserId}. Title: '{ToDoTitle}', Status: {ToDoStatus}", id, userId, toDo.Title, toDo.Status);
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Full ToDoItem data for {ToDoItemId} for User {UserId}: {@ToDoItem}", id, userId, toDo);
                }
                return Ok(toDo);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "ToDoItem {ToDoItemId} not found for User {UserId}.", id, userId ?? "Unknown");
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access by User {UserId} for ToDoItem {ToDoItemId}.", userId ?? "Unknown", id);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} getting ToDoItem {ToDoItemId}.", userId ?? "Unknown", id);
                return StatusCode(500, new { message = "Görev alınırken bir hata oluştu." });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] DateTime? forDate)
        {
            string? userId = null;
            try
            {
                userId = GetCurrentUserId();
                _logger.LogDebug("User {UserId} attempting to get all ToDoItems with forDate filter: {ForDate}", userId, forDate);
                var items = await _toDoService.GetAllAsync(userId, forDate);
                _logger.LogInformation("Successfully retrieved {ToDoItemCount} ToDoItems for User {UserId} with forDate filter: {ForDate}.", items.Count(), userId, forDate);
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("ToDoItems for User {UserId} (forDate: {ForDate}): {@ToDoItems}", userId, forDate, items);
                }
                return Ok(items);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt by User {UserId} while getting all ToDoItems.", userId ?? "Unknown");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} getting all ToDoItems.", userId ?? "Unknown");
                return StatusCode(500, new { message = "Görevler alınırken bir hata oluştu." });
            }
        }

        [HttpGet("month-overview")]
        public async Task<IActionResult> GetMonthOverview([FromQuery] int year, [FromQuery] int month)
        {
            string? userId = null;
            try
            {
                userId = GetCurrentUserId();
                _logger.LogDebug("User {UserId} attempting to get ToDo month overview for Year: {Year}, Month: {Month}.", userId, year, month);
                if (month < 1 || month > 12)
                {
                    return BadRequest(new { message = "Month must be between 1 and 12." });
                }
                if (year < 1 || year > 9999)
                {
                    return BadRequest(new { message = "Year is out of valid range." });
                }

                var overview = await _toDoService.GetMonthOverviewAsync(userId, year, month);
                _logger.LogInformation("Successfully retrieved {Count} date summaries for User {UserId}, Year: {Year}, Month: {Month}.", overview.Count, userId, year, month);
                return Ok(overview);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt by User {UserId} while getting month overview.", userId ?? "Unknown");
                return Unauthorized(new { message = ex.Message });
            }
            catch (ArgumentOutOfRangeException ex)
            {
                _logger.LogWarning(ex, "Argument error while User {UserId} getting month overview. Year: {Year}, Month: {Month}", userId ?? "Unknown", year, month);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} getting month overview. Year: {Year}, Month: {Month}", userId ?? "Unknown", year, month);
                return StatusCode(500, new { message = "Aylık görev özeti alınırken bir hata oluştu." });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateToDoItemDto dto)
        {
            string? userId = null;
            try
            {
                userId = GetCurrentUserId();
                _logger.LogDebug("User {UserId} attempting to update ToDoItem {ToDoItemId} with DTO: {@UpdateToDoItemDto}", userId, id, dto);
                await _toDoService.UpdateAsync(id, dto, userId);
                _logger.LogInformation("User {UserId} successfully updated ToDoItem {ToDoItemId}.", userId, id);
                return Ok(new { message = "To-Do item updated successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "ToDoItem {ToDoItemId} not found for update by User {UserId}. DTO: {@UpdateToDoItemDto}", id, userId ?? "Unknown", dto);
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized update attempt by User {UserId} for ToDoItem {ToDoItemId}. DTO: {@UpdateToDoItemDto}", userId ?? "Unknown", id, dto);
                return Unauthorized(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Argument error while User {UserId} updating ToDoItem {ToDoItemId}. DTO: {@UpdateToDoItemDto}", userId ?? "Unknown", id, dto);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} updating ToDoItem {ToDoItemId}. DTO: {@UpdateToDoItemDto}", userId ?? "Unknown", id, dto);
                return StatusCode(500, new { message = "Görev güncellenirken bir hata oluştu." });
            }
        }


        [HttpPatch("{id}/complete")]
        public async Task<IActionResult> MarkAsCompleted(int id)
        {
            string? userId = null;
            try
            {
                userId = GetCurrentUserId();
                _logger.LogDebug("User {UserId} attempting to mark ToDoItem {ToDoItemId} as completed.", userId, id);
                await _toDoService.MarkToDoManuallyCompletedAsync(id, userId);
                _logger.LogInformation("User {UserId} successfully marked ToDoItem {ToDoItemId} as completed.", userId, id);
                return Ok(new { message = "To-Do item marked as completed successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "ToDoItem {ToDoItemId} not found for marking as completed by User {UserId}.", id, userId ?? "Unknown");
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized attempt by User {UserId} to mark ToDoItem {ToDoItemId} as completed.", userId ?? "Unknown", id);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} marking ToDoItem {ToDoItemId} as completed.", userId ?? "Unknown", id);
                return StatusCode(500, new { message = "Görev tamamlandı olarak işaretlenirken bir hata oluştu." });
            }
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            string? userId = null;
            try
            {
                userId = GetCurrentUserId();
                _logger.LogDebug("User {UserId} attempting to delete ToDoItem {ToDoItemId}.", userId, id);
                await _toDoService.DeleteAsync(id, userId);
                _logger.LogInformation("User {UserId} successfully deleted ToDoItem {ToDoItemId}.", userId, id);
                return Ok(new { message = "To-Do item deleted successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "ToDoItem {ToDoItemId} not found for delete by User {UserId}.", id, userId ?? "Unknown");
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized delete attempt by User {UserId} for ToDoItem {ToDoItemId}.", userId ?? "Unknown", id);
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while User {UserId} deleting ToDoItem {ToDoItemId}.", userId ?? "Unknown", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} deleting ToDoItem {ToDoItemId}.", userId ?? "Unknown", id);
                return StatusCode(500, new { message = "Görev silinirken bir hata oluştu." });
            }
        }
    }
}
