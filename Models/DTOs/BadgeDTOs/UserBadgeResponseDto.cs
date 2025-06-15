using System;

namespace ClimbUpAPI.Models.DTOs.BadgeDTOs
{
    public class UserBadgeResponseDto
    {
        public int UserBadgeID { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime DateAchieved { get; set; }

        public int BadgeDefinitionID { get; set; }
        public string BadgeCoreName { get; set; } = string.Empty;
        public BadgeLevelResponseDto AchievedLevel { get; set; } = null!;
    }
}
