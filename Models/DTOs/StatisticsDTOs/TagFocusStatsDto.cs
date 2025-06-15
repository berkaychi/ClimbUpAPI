namespace ClimbUpAPI.Models.DTOs.StatisticsDTOs
{
    public class TagFocusStatsDto
    {
        public int TagId { get; set; }
        public string TagName { get; set; } = null!;
        public string TagColor { get; set; } = null!;
        public long TotalFocusDurationSeconds { get; set; }
        public int TotalCompletedSessions { get; set; }
    }
}
