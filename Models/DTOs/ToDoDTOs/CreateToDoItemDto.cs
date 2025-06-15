using System.ComponentModel.DataAnnotations;

namespace ClimbUpAPI.Models.DTOs.ToDoDTOs
{
    public class CreateToDoItemDto
    {
        [Required]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public DateTime ForDate { get; set; }

        public DateTime? UserIntendedStartTime { get; set; }
        public List<int>? TagIds { get; set; }
        public TimeSpan? TargetWorkDuration { get; set; }
    }
}
