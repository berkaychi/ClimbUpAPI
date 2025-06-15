using System.ComponentModel.DataAnnotations;

namespace ClimbUpAPI.Models.DTOs.Admin.SessionTypeDTOs
{
    public class AdminCreateSessionTypeDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        [Range(60, 10800)]
        public int WorkDuration { get; set; }

        [Range(0, 3600)]
        public int? BreakDuration { get; set; }

        [Range(1, 100)]
        public int? NumberOfCycles { get; set; }

        public bool IsSystemDefined { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }
}