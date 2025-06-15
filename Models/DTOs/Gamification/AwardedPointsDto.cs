namespace ClimbUpAPI.Models.DTOs.Gamification
{
    public class AwardedPointsDto
    {
        public int EarnedSteps { get; set; }
        public int EarnedStepstones { get; set; }
        public string? Notification { get; set; }
    }
}