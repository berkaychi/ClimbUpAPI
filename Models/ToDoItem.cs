using System.ComponentModel.DataAnnotations;
using ClimbUpAPI.Models.Enums;

namespace ClimbUpAPI.Models
{
    public class ToDoItem
    {
        public int Id { get; set; }
        [Required]
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime ForDate { get; set; }
        public DateTime? UserIntendedStartTime { get; set; }
        public ToDoStatus Status { get; set; } = ToDoStatus.Open;
        public DateTime? AutoCompletedAt { get; set; }
        public DateTime? ManuallyCompletedAt { get; set; }

        public TimeSpan? TargetWorkDuration { get; set; }
        public TimeSpan AccumulatedWorkDuration { get; set; } = TimeSpan.Zero;

        public string UserId { get; set; } = null!;
        public virtual AppUser User { get; set; } = null!;

        public virtual ICollection<ToDoTag> ToDoTags { get; set; } = [];
        public virtual ICollection<FocusSession> FocusSessions { get; set; } = [];
    }
}
