using ClimbUpAPI.Data;
using ClimbUpAPI.Models;
using ClimbUpAPI.Models.DTOs;
using ClimbUpAPI.Models.Enums;
using ClimbUpAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClimbUpAPI.Services.Implementations
{
    public class LeaderboardService : ILeaderboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LeaderboardService> _logger;

        public LeaderboardService(ApplicationDbContext context, ILogger<LeaderboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<LeaderboardResponseDto> GetLeaderboardAsync(LeaderboardQueryParametersDto queryParameters)
        {
            _logger.LogInformation(
                "Fetching leaderboard with parameters: SortBy='{SortBy}', Period='{Period}', Limit={Limit}, Page={Page}",
                queryParameters.SortBy, queryParameters.Period, queryParameters.Limit, queryParameters.Page
            );

            List<LeaderboardEntryDto> entries;
            int totalEntries;

            switch (queryParameters.Period)
            {
                case LeaderboardPeriod.AllTime:
                    (entries, totalEntries) = await GetLeaderboardForAllTimeAsync(queryParameters);
                    break;
                case LeaderboardPeriod.CurrentWeek:
                case LeaderboardPeriod.CurrentMonth:
                    (entries, totalEntries) = await GetLeaderboardForPeriodAsync(queryParameters);
                    break;
                default:
                    _logger.LogWarning("Invalid period parameter received: {PeriodValue}", queryParameters.Period);
                    throw new ArgumentException("Invalid period parameter.", nameof(queryParameters.Period));
            }

            var totalPages = (int)Math.Ceiling(totalEntries / (double)queryParameters.Limit);

            _logger.LogInformation(
                "Successfully retrieved {EntriesCount} leaderboard entries. Period: {Period}, Page: {CurrentPage}/{TotalPages}, TotalEntries: {TotalEntries}",
                entries.Count, queryParameters.Period, queryParameters.Page, totalPages, totalEntries
            );

            return new LeaderboardResponseDto
            {
                Entries = entries,
                TotalEntries = totalEntries,
                TotalPages = totalPages,
                CurrentPage = queryParameters.Page,
                PageSize = queryParameters.Limit
            };
        }

        private async Task<(List<LeaderboardEntryDto> Entries, int TotalEntries)> GetLeaderboardForAllTimeAsync(LeaderboardQueryParametersDto queryParameters)
        {
            var query = _context.UserStats.AsQueryable();

            switch (queryParameters.SortBy)
            {
                case LeaderboardSortCriteria.TotalFocusDuration:
                    query = query.OrderByDescending(us => us.TotalFocusDurationSeconds);
                    break;
                case LeaderboardSortCriteria.TotalCompletedSessions:
                    query = query.OrderByDescending(us => us.TotalCompletedSessions);
                    break;
                default:
                    throw new ArgumentException("Invalid sortBy parameter.", nameof(queryParameters.SortBy));
            }

            var totalEntries = await query.CountAsync();

            var userStatsList = await query
                .Skip((queryParameters.Page - 1) * queryParameters.Limit)
                .Take(queryParameters.Limit)
                .Include(us => us.User)
                .ToListAsync();

            var entries = new List<LeaderboardEntryDto>();
            int rankStart = (queryParameters.Page - 1) * queryParameters.Limit + 1;

            for (int i = 0; i < userStatsList.Count; i++)
            {
                var userStat = userStatsList[i];
                var score = GetScoreForAllTime(userStat, queryParameters.SortBy);
                var formattedScore = FormatScoreForAllTime(score, queryParameters.SortBy, userStat);

                entries.Add(new LeaderboardEntryDto
                {
                    Rank = rankStart + i,
                    UserId = userStat.UserId,
                    FullName = userStat.User?.FullName ?? "N/A",
                    ProfilePictureUrl = userStat.User?.ProfilePictureUrl,
                    Score = score,
                    FormattedScore = formattedScore
                });
            }
            return (entries, totalEntries);
        }

        private async Task<(List<LeaderboardEntryDto> Entries, int TotalEntries)> GetLeaderboardForPeriodAsync(LeaderboardQueryParametersDto queryParameters)
        {
            var (startDate, endDate) = GetDateRangeForPeriod(queryParameters.Period);

            var focusSessionsQuery = _context.FocusSessions
                .Where(fs => fs.EndTime >= startDate && fs.EndTime < endDate &&
                             (fs.Status == SessionState.Completed || fs.Status == SessionState.Cancelled));

            var userPeriodStatsQuery = focusSessionsQuery
                .GroupBy(fs => fs.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalFocusDurationSeconds = g.Sum(fs => fs.TotalWorkDuration),
                    TotalCompletedSessions = g.Count(fs => fs.Status == SessionState.Completed)
                });

            var queryWithUserDetails = userPeriodStatsQuery
                .Join(_context.Users,
                      stat => stat.UserId,
                      user => user.Id,
                      (stat, user) => new
                      {
                          stat.UserId,
                          user.FullName,
                          user.ProfilePictureUrl,
                          stat.TotalFocusDurationSeconds,
                          stat.TotalCompletedSessions
                      });

            var orderedQuery = queryParameters.SortBy == LeaderboardSortCriteria.TotalFocusDuration
                ? queryWithUserDetails.OrderByDescending(s => s.TotalFocusDurationSeconds)
                : queryWithUserDetails.OrderByDescending(s => s.TotalCompletedSessions);

            var totalEntries = await orderedQuery.CountAsync();

            var pagedStats = await orderedQuery
                .Skip((queryParameters.Page - 1) * queryParameters.Limit)
                .Take(queryParameters.Limit)
                .ToListAsync();

            var entries = new List<LeaderboardEntryDto>();
            int rankStart = (queryParameters.Page - 1) * queryParameters.Limit + 1;

            for (int i = 0; i < pagedStats.Count; i++)
            {
                var stat = pagedStats[i];
                long score = queryParameters.SortBy == LeaderboardSortCriteria.TotalFocusDuration
                    ? stat.TotalFocusDurationSeconds
                    : stat.TotalCompletedSessions;

                entries.Add(new LeaderboardEntryDto
                {
                    Rank = rankStart + i,
                    UserId = stat.UserId,
                    FullName = stat.FullName ?? "N/A",
                    ProfilePictureUrl = stat.ProfilePictureUrl,
                    Score = score,
                    FormattedScore = FormatScoreForPeriod(score, queryParameters.SortBy)
                });
            }

            return (entries, totalEntries);
        }


        private (DateTime StartDate, DateTime EndDate) GetDateRangeForPeriod(LeaderboardPeriod period)
        {
            DateTime now = DateTime.UtcNow;
            DateTime startDate;
            DateTime endDate;

            switch (period)
            {
                case LeaderboardPeriod.CurrentWeek:
                    int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                    var weekStart = now.AddDays(-1 * diff).Date;
                    startDate = new DateTime(weekStart.Year, weekStart.Month, weekStart.Day, 0, 0, 0, DateTimeKind.Utc);
                    endDate = startDate.AddDays(7);
                    break;
                case LeaderboardPeriod.CurrentMonth:
                    var monthStart = new DateTime(now.Year, now.Month, 1);
                    startDate = new DateTime(monthStart.Year, monthStart.Month, monthStart.Day, 0, 0, 0, DateTimeKind.Utc);
                    endDate = startDate.AddMonths(1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(period), "Unsupported period for date range calculation.");
            }
            return (startDate, endDate);
        }

        private long GetScoreForAllTime(UserStats userStat, LeaderboardSortCriteria sortBy)
        {
            return sortBy switch
            {
                LeaderboardSortCriteria.TotalFocusDuration => userStat.TotalFocusDurationSeconds,
                LeaderboardSortCriteria.TotalCompletedSessions => userStat.TotalCompletedSessions,
                _ => 0
            };
        }

        private string FormatScoreForAllTime(long score, LeaderboardSortCriteria sortBy, UserStats userStat)
        {
            if (sortBy == LeaderboardSortCriteria.TotalFocusDuration)
            {
                TimeSpan timeSpan = TimeSpan.FromSeconds(userStat.TotalFocusDurationSeconds);
                return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";
            }
            return score.ToString();
        }

        private string FormatScoreForPeriod(long score, LeaderboardSortCriteria sortBy)
        {
            if (sortBy == LeaderboardSortCriteria.TotalFocusDuration)
            {
                TimeSpan timeSpan = TimeSpan.FromSeconds(score);
                return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";
            }
            return score.ToString();
        }
    }
}