using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClimbUpAPI.Models
{
    public class AccountDeletionRequest
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = null!;
        public virtual AppUser User { get; set; } = null!;

        [Required]
        [StringLength(128)]
        public string Token { get; set; } = null!;
        public DateTime ExpirationDate { get; set; }
        public bool IsConfirmed { get; set; } = false;
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public DateTime? ConfirmationDate { get; set; }
    }
}