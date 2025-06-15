using ClimbUpAPI.Data;
using ClimbUpAPI.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ClimbUpAPI.Services.Implementations
{
    public class UserFocusAnalyticsService : IUserFocusAnalyticsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserFocusAnalyticsService> _logger;
        private readonly IConfiguration _configuration;
        private const long StreakMinimumDurationSeconds = 25 * 60;

        public UserFocusAnalyticsService(ApplicationDbContext context, ILogger<UserFocusAnalyticsService> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<UserStats> GetOrCreateUserStatsAsync(string userId)
        {
            var userStats = await _context.UserStats.FindAsync(userId);
            if (userStats == null)
            {
                _logger.LogInformation("No existing UserStats found for User {UserId} in GetOrCreateUserStatsAsync. Creating new record.", userId);
                userStats = new UserStats { UserId = userId };
                _context.UserStats.Add(userStats);
            }
            return userStats;
        }

        public async Task UpdateStatisticsOnSessionCompletionAsync(FocusSession completedSession)
        {
            if (completedSession == null || !completedSession.EndTime.HasValue)
            {
                _logger.LogError("UpdateStatisticsOnSessionCompletionAsync called with null session or session without EndTime. SessionId: {SessionId} in UserFocusAnalyticsService", completedSession?.Id);
                return;
            }

            string userId = completedSession.UserId;
            _logger.LogDebug("Updating session completion specific statistics for User {UserId}, Session {SessionId}. TotalWorkDuration: {TotalWorkDuration}s in UserFocusAnalyticsService",
                userId, completedSession.Id, completedSession.TotalWorkDuration);

            var userStats = await GetOrCreateUserStatsAsync(userId);

            userStats.TotalCompletedSessions++;

            long currentSessionTotalWorkDurationSeconds = completedSession.TotalWorkDuration;
            if (currentSessionTotalWorkDurationSeconds > 0)
            {
                if (currentSessionTotalWorkDurationSeconds > userStats.LongestSingleSessionDurationSeconds)
                {
                    userStats.LongestSingleSessionDurationSeconds = currentSessionTotalWorkDurationSeconds;
                    _logger.LogDebug("User {UserId}: New longest single session (based on TotalWorkDuration): {LongestSingleSessionDurationSeconds}s in UserFocusAnalyticsService", userId, userStats.LongestSingleSessionDurationSeconds);
                }
            }
            userStats.LastSessionCompletionDate = completedSession.EndTime;
            _logger.LogInformation("Session completion statistics (TotalCompleted, LongestSession) updated for User {UserId}, Session {SessionId} in UserFocusAnalyticsService. TotalCompletedSessions: {TotalCompletedSessions}, LongestSingleSessionDurationSeconds: {LongestSingleSessionDurationSeconds}",
                userId, completedSession.Id, userStats.TotalCompletedSessions, userStats.LongestSingleSessionDurationSeconds);
        }

        public async Task UpdateStatsForFocusCompletedToDoAsync(string userId, int toDoItemId)
        {
            var userStats = await GetOrCreateUserStatsAsync(userId);
            userStats.TotalToDosCompletedWithFocus++;
            _logger.LogInformation("User {UserId} completed ToDo {ToDoItemId} with focus. TotalToDosCompletedWithFocus incremented to {TotalToDosCompletedWithFocus} in UserFocusAnalyticsService.",
                userId, toDoItemId, userStats.TotalToDosCompletedWithFocus);
        }

        public async Task UpdateStatsForManualToDoCompletionAsync(string userId, int toDoItemId)
        {
            var userStats = await GetOrCreateUserStatsAsync(userId);
            userStats.TotalToDosCompleted++;
            _logger.LogInformation("User {UserId} completed ToDo {ToDoItemId} manually. TotalToDosCompleted incremented to {TotalToDosCompleted} in UserFocusAnalyticsService.",
                userId, toDoItemId, userStats.TotalToDosCompleted);
        }

        public async Task UpdateStatsForWorkPhaseAsync(string userId, long workPhaseDurationSeconds, DateTime phaseEndTime)
        {
            if (workPhaseDurationSeconds <= 0)
            {
                _logger.LogDebug("User {UserId}: Work phase duration is zero or negative ({WorkPhaseDurationSeconds}s) in UserFocusAnalyticsService. Stats not updated for this phase.", userId, workPhaseDurationSeconds);
                return;
            }

            var userStats = await GetOrCreateUserStatsAsync(userId);

            userStats.TotalFocusDurationSeconds += workPhaseDurationSeconds;
            _logger.LogDebug("User {UserId}: TotalFocusDurationSeconds increased by {WorkPhaseDurationSeconds}s from work phase. New total: {TotalFocusDurationSeconds}s in UserFocusAnalyticsService", userId, workPhaseDurationSeconds, userStats.TotalFocusDurationSeconds);

            if (workPhaseDurationSeconds >= StreakMinimumDurationSeconds)
            {
                DateTime today = phaseEndTime.Date;
                DateTime? lastMeaningfulCompletionDate = userStats.LastSessionCompletionDate?.Date;

                _logger.LogDebug("User {UserId} (Streak Check - Work Phase >= {MinDuration}s): Duration {WorkPhaseDurationSeconds}s, PhaseEndTime: {PhaseEndTime}, LastMeaningfulCompletion: {LastMeaningfulCompletionDate}, CurrentStreak: {CurrentStreak}, LongestStreak: {LongestStreak} in UserFocusAnalyticsService",
                    userId, StreakMinimumDurationSeconds, workPhaseDurationSeconds, phaseEndTime, lastMeaningfulCompletionDate, userStats.CurrentStreakDays, userStats.LongestStreakDays);

                if (lastMeaningfulCompletionDate == today)
                {
                    _logger.LogDebug("User {UserId}: Meaningful work phase completed today. Streak already potentially counted for today or will be by another phase/session in UserFocusAnalyticsService.", userId);
                }
                else if (lastMeaningfulCompletionDate == today.AddDays(-1))
                {
                    userStats.CurrentStreakDays++;
                    _logger.LogDebug("User {UserId}: Meaningful work phase streak continued. New current streak: {CurrentStreakDays} in UserFocusAnalyticsService", userId, userStats.CurrentStreakDays);
                }
                else
                {
                    userStats.CurrentStreakDays = 1;
                    _logger.LogDebug("User {UserId}: Meaningful work phase streak reset or first meaningful phase. New current streak: {CurrentStreakDays} in UserFocusAnalyticsService", userId, userStats.CurrentStreakDays);
                }

                if (userStats.CurrentStreakDays > userStats.LongestStreakDays)
                {
                    userStats.LongestStreakDays = userStats.CurrentStreakDays;
                    _logger.LogDebug("User {UserId}: New longest meaningful streak from work phase: {LongestStreakDays} in UserFocusAnalyticsService", userId, userStats.LongestStreakDays);
                }
                userStats.LastSessionCompletionDate = phaseEndTime.Date;
            }
            else
            {
                _logger.LogDebug("User {UserId}: Work phase duration {WorkPhaseDurationSeconds}s is less than minimum {MinDurationSeconds}s for streak. Streak not affected by this phase in UserFocusAnalyticsService.",
                    userId, workPhaseDurationSeconds, StreakMinimumDurationSeconds);
            }
            _logger.LogInformation("User statistics updated for User {UserId} after work phase. Duration: {WorkPhaseDurationSeconds}s in UserFocusAnalyticsService", userId, workPhaseDurationSeconds);
        }

        public async Task UpdateStatsOnSessionCreationAsync(string userId)
        {
            var userStats = await GetOrCreateUserStatsAsync(userId);
            userStats.TotalStartedSessions++;
            _logger.LogInformation("User {UserId} started a new session. TotalStartedSessions incremented to {TotalStartedSessions} in UserFocusAnalyticsService.", userId, userStats.TotalStartedSessions);
        }

        public async Task UpdateUsageScoresAsync(string userId, int? sessionTypeId, List<int>? tagIds)
        {
            try
            {
                double alpha = _configuration.GetValue<double>("PriorityTrackingSettings:Alpha", 0.2);
                DateTime now = DateTime.UtcNow;

                if (sessionTypeId.HasValue)
                {
                    var sessionTypeUsage = await GetOrCreateSessionTypeUsageAsync(userId, sessionTypeId.Value);
                    sessionTypeUsage.Score = (alpha * 1.0) + (1.0 - alpha) * sessionTypeUsage.Score;
                    sessionTypeUsage.LastUsedDate = now;
                    _logger.LogDebug("User {UserId}: UserSessionTypeUsage updated for SessionType {SessionTypeId}. New score: {Score} in UserFocusAnalyticsService.", userId, sessionTypeId.Value, sessionTypeUsage.Score);
                }

                if (tagIds != null && tagIds.Any())
                {
                    foreach (var tagId in tagIds)
                    {
                        var tagUsage = await GetOrCreateTagUsageAsync(userId, tagId);
                        tagUsage.Score = (alpha * 1.0) + (1.0 - alpha) * tagUsage.Score;
                        tagUsage.LastUsedDate = now;
                        _logger.LogDebug("User {UserId}: UserTagUsage updated for Tag {TagId}. New score: {Score} in UserFocusAnalyticsService.", userId, tagId, tagUsage.Score);
                    }
                }
                _logger.LogInformation("User {UserId} usage scores updated for SessionType {SessionTypeId} and Tags {TagCount} in UserFocusAnalyticsService.", userId, sessionTypeId, tagIds?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating usage scores for User {UserId}, SessionType {SessionTypeId}, Tags {TagCount} in UserFocusAnalyticsService.", userId, sessionTypeId, tagIds?.Count ?? 0);
            }
        }

        public async Task<UserSessionTypeUsage> GetOrCreateSessionTypeUsageAsync(string userId, int sessionTypeId)
        {
            var usageRecord = _context.UserSessionTypeUsages.Local
                .FirstOrDefault(u => u.UserId == userId && u.SessionTypeId == sessionTypeId);

            if (usageRecord != null)
            {
                _logger.LogDebug("Found UserSessionTypeUsage for User {UserId}, SessionType {SessionTypeId} in local context.", userId, sessionTypeId);
                return usageRecord;
            }

            usageRecord = await _context.UserSessionTypeUsages
                .FirstOrDefaultAsync(u => u.UserId == userId && u.SessionTypeId == sessionTypeId);

            if (usageRecord == null)
            {
                _logger.LogDebug("No UserSessionTypeUsage found for User {UserId}, SessionType {SessionTypeId}. Creating new record.", userId, sessionTypeId);
                usageRecord = new UserSessionTypeUsage
                {
                    UserId = userId,
                    SessionTypeId = sessionTypeId,
                    LastUsedDate = DateTime.UtcNow,
                    Score = 1.0
                };
                _context.UserSessionTypeUsages.Add(usageRecord);
            }

            return usageRecord;
        }

        public async Task<UserTagUsage> GetOrCreateTagUsageAsync(string userId, int tagId)
        {
            var usageRecord = _context.UserTagUsages.Local
                .FirstOrDefault(u => u.UserId == userId && u.TagId == tagId);

            if (usageRecord != null)
            {
                _logger.LogDebug("Found UserTagUsage for User {UserId}, Tag {TagId} in local context.", userId, tagId);
                return usageRecord;
            }

            usageRecord = await _context.UserTagUsages
                .FirstOrDefaultAsync(u => u.UserId == userId && u.TagId == tagId);

            if (usageRecord == null)
            {
                _logger.LogDebug("No UserTagUsage found for User {UserId}, Tag {TagId}. Creating new record.", userId, tagId);
                usageRecord = new UserTagUsage
                {
                    UserId = userId,
                    TagId = tagId,
                    LastUsedDate = DateTime.UtcNow,
                    Score = 1.0
                };
                _context.UserTagUsages.Add(usageRecord);
            }

            return usageRecord;
        }
    }
}