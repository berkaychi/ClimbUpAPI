namespace ClimbUpAPI.Models.DTOs.StatisticsDTOs
{
    public class PeriodFocusStatsDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public long TotalFocusDurationSeconds { get; set; }
        public int TotalCompletedSessions { get; set; }

    }
}
