using System.ComponentModel.DataAnnotations;


namespace ClimbUpAPI.Models
{
    public class SessionType
    {
        public int Id { get; set; }

        public string? UserId { get; set; }
        public virtual AppUser? User { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        [Range(60, 10800)]
        public int WorkDuration { get; set; }

        [Range(0, 3600)]
        public int? BreakDuration { get; set; }


        public int? NumberOfCycles { get; set; }

        public bool IsSystemDefined { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public virtual ICollection<FocusSession> FocusSessions { get; set; } = [];
    }
}