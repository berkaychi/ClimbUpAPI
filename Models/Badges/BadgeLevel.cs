using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClimbUpAPI.Models.Badges
{
    public class BadgeLevel
    {
        [Key]
        public int BadgeLevelID { get; set; }

        [Required]
        public int BadgeDefinitionID { get; set; }
        [ForeignKey("BadgeDefinitionID")]
        public virtual BadgeDefinition? BadgeDefinition { get; set; }

        [Required]
        public int Level { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        [Required]
        [StringLength(255)]
        public required string Description { get; set; }

        [Required]
        [StringLength(255)]
        public required string IconURL { get; set; }
        [Required]
        public int RequiredValue { get; set; }

        [Required]
        public int AwardedSteps { get; set; }

        public virtual ICollection<UserBadge> UserBadges { get; set; }

        public BadgeLevel()
        {
            UserBadges = new HashSet<UserBadge>();
        }
    }
}
