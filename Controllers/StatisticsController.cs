using ClimbUpAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClimbUpAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly IStatisticsService _statisticsService;
        private readonly ILogger<StatisticsController> _logger;

        public StatisticsController(IStatisticsService statisticsService, ILogger<StatisticsController> logger)
        {
            _statisticsService = statisticsService;
            _logger = logger;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token.");

        [HttpGet("summary")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSummaryStats()
        {
            string? userId = null;
            try
            {
                userId = GetUserId();
                _logger.LogDebug("User {UserId} attempting to get summary stats.", userId);
                var stats = await _statisticsService.GetUserStatsSummaryAsync(userId);
                if (stats == null)
                {
                    _logger.LogWarning("Summary stats not found for User {UserId}.", userId);
                    return NotFound(new { message = "Kullanıcı için istatistik bulunamadı." });
                }
                _logger.LogInformation("Successfully retrieved summary stats for User {UserId}. TotalFocusDurationSeconds: {TotalFocusDurationSeconds}, TotalCompletedSessions: {TotalCompletedSessions}", userId, stats.TotalFocusDurationSeconds, stats.TotalCompletedSessions);
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Full summary stats for User {UserId}: {@UserStatsSummary}", userId, stats);
                }
                return Ok(stats);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt while getting summary stats. UserIdAttempted: {UserIdAttempted}", userId ?? "Unknown");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} getting summary stats.", userId ?? "Unknown");
                return StatusCode(500, new { message = "İstatistik özeti alınırken bir hata oluştu." });
            }
        }

        [HttpGet("period")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPeriodStats([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            string? userId = null;
            if (startDate > endDate)
            {
                _logger.LogWarning("Invalid date range for period stats request. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                return BadRequest(new { message = "Başlangıç tarihi, bitiş tarihinden sonra olamaz." });
            }

            try
            {
                userId = GetUserId();
                _logger.LogDebug("User {UserId} attempting to get period stats. StartDate: {StartDate}, EndDate: {EndDate}", userId, startDate, endDate);
                var stats = await _statisticsService.GetPeriodFocusStatsAsync(userId, startDate, endDate);
                _logger.LogInformation("Successfully retrieved period stats for User {UserId}. StartDate: {StartDate}, EndDate: {EndDate}, TotalCompletedSessions: {TotalCompletedSessions}", userId, startDate, endDate, stats.TotalCompletedSessions);
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Period stats for User {UserId}: {@PeriodStats}", userId, stats);
                }
                return Ok(stats);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt while getting period stats. UserIdAttempted: {UserIdAttempted}, StartDate: {StartDate}, EndDate: {EndDate}", userId ?? "Unknown", startDate, endDate);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} getting period stats. StartDate: {StartDate}, EndDate: {EndDate}", userId ?? "Unknown", startDate, endDate);
                return StatusCode(500, new { message = "Dönemsel istatistikler alınırken bir hata oluştu." });
            }
        }


        [HttpGet("tags")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTagStats([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            string? userId = null;
            if (startDate > endDate)
            {
                _logger.LogWarning("Invalid date range for tag stats request. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                return BadRequest(new { message = "Başlangıç tarihi, bitiş tarihinden sonra olamaz." });
            }

            try
            {
                userId = GetUserId();
                _logger.LogDebug("User {UserId} attempting to get tag stats. StartDate: {StartDate}, EndDate: {EndDate}", userId, startDate, endDate);
                var stats = await _statisticsService.GetTagFocusStatsAsync(userId, startDate, endDate);
                _logger.LogInformation("Successfully retrieved tag stats for User {UserId}. StartDate: {StartDate}, EndDate: {EndDate}, TagStatsCount: {TagStatsCount}", userId, startDate, endDate, stats.Count());
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Tag stats for User {UserId}: {@TagStats}", userId, stats);
                }
                return Ok(stats);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt while getting tag stats. UserIdAttempted: {UserIdAttempted}, StartDate: {StartDate}, EndDate: {EndDate}", userId ?? "Unknown", startDate, endDate);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} getting tag stats. StartDate: {StartDate}, EndDate: {EndDate}", userId ?? "Unknown", startDate, endDate);
                return StatusCode(500, new { message = "Etiket istatistikleri alınırken bir hata oluştu." });
            }
        }

        [HttpGet("daily")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDailyStats([FromQuery] DateTime date)
        {
            string? userId = null;
            try
            {
                userId = GetUserId();
                _logger.LogDebug("User {UserId} attempting to get daily stats for Date {Date}", userId, date.Date);
                var stats = await _statisticsService.GetDailyFocusStatsAsync(userId, date.Date);
                _logger.LogInformation("Successfully retrieved daily stats for User {UserId} on Date {Date}. Duration: {Duration}, Sessions: {Sessions}",
                    userId, date.Date, stats?.TotalFocusDurationSecondsToday, stats?.CompletedSessionsToday);
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Full daily stats for User {UserId}, Date {Date}: {@DailyStats}", userId, date.Date, stats);
                }
                return Ok(stats);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt while getting daily stats. UserIdAttempted: {UserIdAttempted}, Date: {Date}", userId ?? "Unknown", date.Date);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} getting daily stats for Date {Date}.", userId ?? "Unknown", date.Date);
                return StatusCode(500, new { message = "Günlük istatistikler alınırken bir hata oluştu." });
            }
        }

        [HttpGet("daily-range")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDailyStatsRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            string? userId = null;
            if (startDate.Date > endDate.Date)
            {
                _logger.LogWarning("Invalid date range for daily stats range request. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                return BadRequest(new { message = "Başlangıç tarihi, bitiş tarihinden sonra olamaz." });
            }

            try
            {
                userId = GetUserId();
                _logger.LogDebug("User {UserId} attempting to get daily stats range. StartDate: {StartDate}, EndDate: {EndDate}", userId, startDate.Date, endDate.Date);
                var stats = await _statisticsService.GetDailyFocusStatsRangeAsync(userId, startDate.Date, endDate.Date);
                _logger.LogInformation("Successfully retrieved {Count} daily stat entries for User {UserId} from {StartDate} to {EndDate}", stats.Count, userId, startDate.Date, endDate.Date);
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Full daily stats range for User {UserId}, StartDate: {StartDate}, EndDate: {EndDate}: {@DailyStatsRange}", userId, startDate.Date, endDate.Date, stats);
                }
                return Ok(stats);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt while getting daily stats range. UserIdAttempted: {UserIdAttempted}, StartDate: {StartDate}, EndDate: {EndDate}", userId ?? "Unknown", startDate.Date, endDate.Date);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} getting daily stats range. StartDate: {StartDate}, EndDate: {EndDate}", userId ?? "Unknown", startDate.Date, endDate.Date);
                return StatusCode(500, new { message = "Günlük aralık istatistikleri alınırken bir hata oluştu." });
            }
        }
    }
}
