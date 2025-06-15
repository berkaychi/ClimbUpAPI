using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClimbUpAPI.Models
{
    public class StoreItem
    {
        [Key]
        public int StoreItemId { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }
        [Required]
        public int PriceSS { get; set; }

        [StringLength(255)]
        public string? IconUrl { get; set; }

        public bool IsConsumable { get; set; } = false;

        public int? MaxQuantityPerUser { get; set; }

        public string? EffectDetails { get; set; }

        public virtual ICollection<UserStoreItem> UserStoreItems { get; set; } = new List<UserStoreItem>();
    }
}