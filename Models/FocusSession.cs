using ClimbUpAPI.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClimbUpAPI.Models
{
    public class FocusSession
    {
        public int Id { get; set; }

        public string UserId { get; set; } = null!;
        public virtual AppUser User { get; set; } = null!;


        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public TimeSpan? CustomDuration { get; set; }

        public SessionState Status { get; set; }

        public DateTime? CurrentStateEndTime { get; set; }
        public int CompletedCycles { get; set; } = 0;
        public int TotalWorkDuration { get; set; } = 0;
        public int TotalBreakDuration { get; set; } = 0;
        public int? SessionTypeId { get; set; }
        public virtual SessionType? SessionType { get; set; } = null!;

        public int? FocusLevel { get; set; }
        public string? ReflectionNotes { get; set; }

        [NotMapped]
        public DateTime? CurrentPhaseActualStartTime { get; set; }


        public int? ToDoItemId { get; set; }
        public virtual ToDoItem? ToDoItem { get; set; }

        public virtual ICollection<FocusSessionTag> FocusSessionTags { get; set; } = new List<FocusSessionTag>();


    }
}
