using ClimbUpAPI.Models.DTOs.TagDTOs;
using ClimbUpAPI.Models.Enums;

namespace ClimbUpAPI.Models.DTOs.SessionDTOs
{
    public class FocusSessionResponseDto
    {
        public int Id { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public SessionState Status { get; set; }

        public int? SessionTypeId { get; set; }
        public string? SessionTypeName { get; set; }
        public int? CustomDurationSeconds { get; set; }

        public int TotalWorkDuration { get; set; }
        public int TotalBreakDuration { get; set; }

        public DateTime? CurrentStateEndTime { get; set; }
        public int CompletedCycles { get; set; }

        public List<TagDto> Tags { get; set; } = [];

        public int? FocusLevel { get; set; }
        public string? ReflectionNotes { get; set; }
    }

}