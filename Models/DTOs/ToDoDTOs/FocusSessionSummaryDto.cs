using System;

namespace ClimbUpAPI.Models.DTOs.ToDoDTOs
{
    public class FocusSessionSummaryDto
    {
        public int Id { get; set; }
        public DateTime ActualStartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } = null!;
        public string? SessionTypeName { get; set; }
    }
}