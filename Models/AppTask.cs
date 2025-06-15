using ClimbUpAPI.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace ClimbUpAPI.Models
{
    public class AppTask
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Title { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public TaskType TaskType { get; set; }

        [Required]
        public int TargetProgress { get; set; }

        public string? Recurrence { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<UserAppTask> UserAppTasks { get; set; } = new List<UserAppTask>();
    }
}