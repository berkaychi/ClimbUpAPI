using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClimbUpAPI.Models.Badges
{
    public class BadgeDefinition
    {
        [Key]
        public int BadgeDefinitionID { get; set; }

        [Required]
        [StringLength(100)]
        public required string CoreName { get; set; }

        [Required]
        [StringLength(100)]
        public required string MetricToTrack { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        public virtual ICollection<BadgeLevel> BadgeLevels { get; set; }

        public BadgeDefinition()
        {
            BadgeLevels = new HashSet<BadgeLevel>();
        }
    }
}
