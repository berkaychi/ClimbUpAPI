using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClimbUpAPI.Models
{
    public class UserStoreItem
    {
        [Required]
        public required string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual AppUser? User { get; set; }

        [Required]
        public int StoreItemId { get; set; }
        [ForeignKey("StoreItemId")]
        public virtual StoreItem? StoreItem { get; set; }

        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
        public int Quantity { get; set; } = 1;
        public bool IsActive { get; set; } = false;
    }
}