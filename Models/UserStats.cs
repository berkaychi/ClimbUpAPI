using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClimbUpAPI.Models
{
    public class UserStats
    {
        [Key]
        [ForeignKey("User")]
        public string UserId { get; set; } = null!;
        public virtual AppUser User { get; set; } = null!;

        public long TotalFocusDurationSeconds { get; set; } = 0;
        public int TotalCompletedSessions { get; set; } = 0;
        public int TotalStartedSessions { get; set; } = 0;
        public long LongestSingleSessionDurationSeconds { get; set; } = 0;
        public int TotalToDosCompletedWithFocus { get; set; } = 0;
        public int CurrentStreakDays { get; set; } = 0;
        public int LongestStreakDays { get; set; } = 0;
        public int TotalToDosCompleted { get; set; } = 0;
        public DateTime? LastSessionCompletionDate { get; set; }
    }
}
