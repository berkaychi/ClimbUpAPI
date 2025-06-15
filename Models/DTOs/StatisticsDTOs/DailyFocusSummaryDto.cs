namespace ClimbUpAPI.Models.DTOs.StatisticsDTOs
{
    public class DailyFocusSummaryDto
    {
        public DateTime Date { get; set; }
        public long TotalFocusDurationSecondsToday { get; set; }
        public int CompletedSessionsToday { get; set; }
    }
}
