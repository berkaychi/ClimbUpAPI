using System.Collections.Generic;

namespace ClimbUpAPI.Models.DTOs.BadgeDTOs
{
    public class BadgeDefinitionResponseDto
    {
        public int BadgeDefinitionID { get; set; }
        public string CoreName { get; set; } = string.Empty;
        public string MetricToTrack { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<BadgeLevelResponseDto> Levels { get; set; } = new List<BadgeLevelResponseDto>();

        public long CurrentUserProgress { get; set; }
        public int? NextLevelRequiredValue { get; set; }
        public int? CurrentAchievedLevel { get; set; }
        public bool IsMaxLevelAchieved { get; set; }
    }
}
