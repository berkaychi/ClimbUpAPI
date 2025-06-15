namespace ClimbUpAPI.Models.DTOs.StatisticsDTOs
{
    public class UserStatsSummaryDto
    {
        public long TotalFocusDurationSeconds { get; set; }
        public int TotalCompletedSessions { get; set; }
        public int TotalStartedSessions { get; set; }
        public double SessionCompletionRate { get; set; }
        public long AverageSessionDurationSeconds { get; set; }
        public long LongestSingleSessionDurationSeconds { get; set; }
        public int TotalToDosCompletedWithFocus { get; set; }
        public int CurrentStreakDays { get; set; }
        public int LongestStreakDays { get; set; }
        public DateTime? LastSessionCompletionDate { get; set; }
    }
}
