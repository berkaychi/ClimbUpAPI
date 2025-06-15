using AutoMapper;
using ClimbUpAPI.Data;
using ClimbUpAPI.Models;
using ClimbUpAPI.Models.DTOs.StatisticsDTOs;
using ClimbUpAPI.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace ClimbUpAPI.Services.Implementations
{
    public class StatisticsService : IStatisticsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<StatisticsService> _logger;

        public StatisticsService(ApplicationDbContext context, IMapper mapper, ILogger<StatisticsService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<UserStatsSummaryDto?> GetUserStatsSummaryAsync(string userId)
        {
            _logger.LogDebug("Attempting to get user stats summary for User {UserId}", userId);
            var userStats = await _context.UserStats.FindAsync(userId);

            if (userStats == null)
            {
                _logger.LogWarning("User stats summary not found for User {UserId}", userId);
                return null;
            }

            var responseDto = _mapper.Map<UserStatsSummaryDto>(userStats);
            _logger.LogInformation("Successfully retrieved user stats summary for User {UserId}. TotalFocusDurationSeconds: {TotalFocusDurationSeconds}, TotalCompletedSessions: {TotalCompletedSessions}",
                userId, responseDto.TotalFocusDurationSeconds, responseDto.TotalCompletedSessions);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Full UserStatsSummaryDto for User {UserId}: {@UserStatsSummaryDto}", userId, responseDto);
            }
            return responseDto;
        }

        public async Task<PeriodFocusStatsDto> GetPeriodFocusStatsAsync(string userId, DateTime startDate, DateTime endDate)
        {
            _logger.LogDebug("Attempting to get period focus stats for User {UserId}. StartDate: {StartDate}, EndDate: {EndDate}", userId, startDate, endDate);
            var endDateInclusive = endDate.Date.AddDays(1);

            var sessionsInPeriodQuery = _context.FocusSessions
                .Where(fs => fs.UserId == userId &&
                             (fs.Status == SessionState.Completed || fs.Status == SessionState.Cancelled) &&
                             fs.EndTime >= startDate.Date &&
                             fs.EndTime < endDateInclusive &&
                             fs.EndTime.HasValue);

            long totalDurationSeconds = 0;
            if (await sessionsInPeriodQuery.AnyAsync())
            {
                totalDurationSeconds = (long)await sessionsInPeriodQuery
                    .SumAsync(fs => fs.TotalWorkDuration);
            }

            int completedSessionsCount = await sessionsInPeriodQuery
                .CountAsync(fs => fs.Status == SessionState.Completed);

            var responseDto = new PeriodFocusStatsDto
            {
                StartDate = startDate.Date,
                EndDate = endDate.Date,
                TotalFocusDurationSeconds = totalDurationSeconds,
                TotalCompletedSessions = completedSessionsCount
            };

            _logger.LogInformation("Successfully retrieved period focus stats for User {UserId}. StartDate: {StartDate}, EndDate: {EndDate}, TotalFocusDurationSeconds: {TotalFocusDurationSeconds}, TotalCompletedSessions: {TotalCompletedSessions}",
                userId, startDate, endDate, responseDto.TotalFocusDurationSeconds, responseDto.TotalCompletedSessions);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Full PeriodFocusStatsDto for User {UserId}, StartDate: {StartDate}, EndDate: {EndDate}: {@PeriodFocusStatsDto}", userId, startDate, endDate, responseDto);
            }
            return responseDto;
        }

        public async Task<List<TagFocusStatsDto>> GetTagFocusStatsAsync(string userId, DateTime startDate, DateTime endDate)
        {
            _logger.LogDebug("Attempting to get tag focus stats for User {UserId}. StartDate: {StartDate}, EndDate: {EndDate}", userId, startDate, endDate);
            var endDateInclusive = endDate.Date.AddDays(1);

            var tagStats = await _context.FocusSessionTags
                .Include(fst => fst.Tag)
                .Include(fst => fst.FocusSession)
                .Where(fst => fst.FocusSession.UserId == userId &&
                              (fst.FocusSession.Status == SessionState.Completed || fst.FocusSession.Status == SessionState.Cancelled) &&
                              fst.FocusSession.EndTime >= startDate.Date &&
                              fst.FocusSession.EndTime < endDateInclusive &&
                              fst.FocusSession.EndTime.HasValue)
                .GroupBy(fst => fst.Tag)
                .Select(g => new TagFocusStatsDto
                {
                    TagId = g.Key.Id,
                    TagName = g.Key.Name,
                    TagColor = g.Key.Color,
                    TotalFocusDurationSeconds = (long)g.Sum(x => x.FocusSession.TotalWorkDuration),
                    TotalCompletedSessions = g.Where(x => x.FocusSession.Status == SessionState.Completed)
                                              .Select(x => x.FocusSessionId)
                                              .Distinct()
                                              .Count()
                })
                .ToListAsync();

            _logger.LogInformation("Successfully retrieved {TagStatsCount} tag focus stats for User {UserId}. StartDate: {StartDate}, EndDate: {EndDate}",
                tagStats.Count, userId, startDate, endDate);
            if (_logger.IsEnabled(LogLevel.Debug) && tagStats.Any())
            {
                _logger.LogDebug("Full TagFocusStatsDto list for User {UserId}, StartDate: {StartDate}, EndDate: {EndDate}: {@TagFocusStatsList}", userId, startDate, endDate, tagStats);
            }
            return tagStats;
        }

        private async Task<DailyFocusSummaryDto> CalculateDailyFocusSummaryAsync(string userId, DateTime date)
        {
            var dayStart = date.Date;
            var dayEnd = date.Date.AddDays(1);

            var sessionsOnDateQuery = _context.FocusSessions
                .Where(fs => fs.UserId == userId &&
                             (fs.Status == SessionState.Completed || fs.Status == SessionState.Cancelled) &&
                             fs.EndTime >= dayStart &&
                             fs.EndTime < dayEnd &&
                             fs.EndTime.HasValue);

            long totalDurationSecondsToday = 0;
            if (await sessionsOnDateQuery.AnyAsync())
            {
                totalDurationSecondsToday = (long)await sessionsOnDateQuery
                    .SumAsync(fs => fs.TotalWorkDuration);
            }

            int completedSessionsToday = await sessionsOnDateQuery
                .CountAsync(fs => fs.Status == SessionState.Completed);

            return new DailyFocusSummaryDto
            {
                Date = dayStart,
                TotalFocusDurationSecondsToday = totalDurationSecondsToday,
                CompletedSessionsToday = completedSessionsToday
            };
        }

        public async Task<DailyFocusSummaryDto?> GetDailyFocusStatsAsync(string userId, DateTime date)
        {
            _logger.LogDebug("Attempting to get daily focus stats for User {UserId} on Date {Date}", userId, date.Date);

            var dailySummary = await CalculateDailyFocusSummaryAsync(userId, date);

            _logger.LogInformation("Successfully retrieved daily focus stats for User {UserId} on Date {Date}. Duration: {Duration}, Sessions: {Sessions}",
                userId, date.Date, dailySummary.TotalFocusDurationSecondsToday, dailySummary.CompletedSessionsToday);

            if (dailySummary.CompletedSessionsToday == 0 && dailySummary.TotalFocusDurationSecondsToday == 0)
            {
                _logger.LogInformation("No completed sessions found for User {UserId} on Date {Date}. Returning stats with zeros (as calculated).", userId, date.Date);
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Full DailyFocusSummaryDto for User {UserId}, Date {Date}: {@DailyFocusSummaryDto}", userId, date.Date, dailySummary);
            }
            return dailySummary;
        }

        public async Task<List<DailyFocusSummaryDto>> GetDailyFocusStatsRangeAsync(string userId, DateTime startDate, DateTime endDate)
        {
            _logger.LogDebug("Attempting to get daily focus stats range for User {UserId}. StartDate: {StartDate}, EndDate: {EndDate}", userId, startDate, endDate);
            var results = new List<DailyFocusSummaryDto>();

            if (startDate > endDate)
            {
                _logger.LogWarning("Invalid date range for daily stats range: StartDate {StartDate} is after EndDate {EndDate} for User {UserId}", startDate, endDate, userId);
                return results;
            }

            for (DateTime date = startDate.Date; date.Date <= endDate.Date; date = date.AddDays(1))
            {
                var dailySummary = await CalculateDailyFocusSummaryAsync(userId, date);
                results.Add(dailySummary);
            }

            _logger.LogInformation("Successfully retrieved {Count} daily focus summaries for User {UserId} from {StartDate} to {EndDate}.", results.Count, userId, startDate.Date, endDate.Date);
            if (_logger.IsEnabled(LogLevel.Debug) && results.Any())
            {
                _logger.LogDebug("Full list of DailyFocusSummaryDto for User {UserId}, Range {StartDate}-{EndDate}: {@DailyFocusSummaryList}", userId, startDate.Date, endDate.Date, results);
            }
            return results;
        }
    }
}
