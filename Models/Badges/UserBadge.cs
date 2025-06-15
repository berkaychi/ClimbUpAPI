using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClimbUpAPI.Models.Badges
{
    public class UserBadge
    {
        [Key]
        public int UserBadgeID { get; set; }

        [Required]
        public string UserId { get; set; } = null!;
        [ForeignKey("UserId")]
        public virtual AppUser User { get; set; } = null!;

        [Required]
        public int BadgeLevelID { get; set; }
        [ForeignKey("BadgeLevelID")]
        public virtual BadgeLevel BadgeLevel { get; set; } = null!;

        [Required]
        public DateTime DateAchieved { get; set; }
    }
}
