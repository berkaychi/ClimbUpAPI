using ClimbUpAPI.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClimbUpAPI.Models
{
    public class UserAppTask
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string AppUserId { get; set; }
        [ForeignKey("AppUserId")]
        public virtual required AppUser AppUser { get; set; }

        [Required]
        public int AppTaskId { get; set; }
        [ForeignKey("AppTaskId")]
        public virtual required AppTask AppTaskDefinition { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }

        public int CurrentProgress { get; set; } = 0;

        public ClimbUpAPI.Models.Enums.TaskStatus Status { get; set; } = ClimbUpAPI.Models.Enums.TaskStatus.Pending;
        public DateTime? CompletedDate { get; set; }
    }
}