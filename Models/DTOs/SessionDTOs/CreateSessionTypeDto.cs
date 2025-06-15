using System.ComponentModel.DataAnnotations;

namespace ClimbUpAPI.Models.DTOs.SessionDTOs
{
    public class CreateSessionTypeDto
    {
        [Required]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        [Range(60, 10800)]
        public int WorkDuration { get; set; }

        [Range(0, 3600)]
        public int? BreakDuration { get; set; }

        [Range(1, 100)]
        public int? NumberOfCycles { get; set; }
    }

}