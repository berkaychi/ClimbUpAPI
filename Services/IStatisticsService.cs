using ClimbUpAPI.Models.DTOs.StatisticsDTOs;

namespace ClimbUpAPI.Services
{
    public interface IStatisticsService
    {
        Task<UserStatsSummaryDto?> GetUserStatsSummaryAsync(string userId);
        Task<PeriodFocusStatsDto> GetPeriodFocusStatsAsync(string userId, DateTime startDate, DateTime endDate);

        Task<List<TagFocusStatsDto>> GetTagFocusStatsAsync(string userId, DateTime startDate, DateTime endDate);

        Task<DailyFocusSummaryDto?> GetDailyFocusStatsAsync(string userId, DateTime date);
        Task<List<DailyFocusSummaryDto>> GetDailyFocusStatsRangeAsync(string userId, DateTime startDate, DateTime endDate);
    }
}
