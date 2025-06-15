namespace ClimbUpAPI.Models.DTOs
{
    public class LeaderboardEntryDto
    {
        public int Rank { get; set; }
        public required string UserId { get; set; }
        public required string FullName { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public long Score { get; set; }
        public string? FormattedScore { get; set; }
    }
}