using ClimbUpAPI.Models;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ClimbUpAPI.Services
{
    public interface IUserFocusAnalyticsService
    {
        Task UpdateStatisticsOnSessionCompletionAsync(FocusSession completedSession);

        Task UpdateStatsForWorkPhaseAsync(string userId, long workPhaseDurationSeconds, DateTime phaseEndTime);

        Task UpdateStatsForFocusCompletedToDoAsync(string userId, int toDoItemId);

        Task UpdateStatsOnSessionCreationAsync(string userId);

        Task<UserStats> GetOrCreateUserStatsAsync(string userId);

        Task UpdateUsageScoresAsync(string userId, int? sessionTypeId, List<int>? tagIds);
        Task UpdateStatsForManualToDoCompletionAsync(string userId, int toDoItemId);
        Task<UserSessionTypeUsage> GetOrCreateSessionTypeUsageAsync(string userId, int sessionTypeId);
        Task<UserTagUsage> GetOrCreateTagUsageAsync(string userId, int tagId);
    }
}