using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClimbUpAPI.Models
{
    public class UserRefreshToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual AppUser User { get; set; } = null!;

        [Required]
        public string Token { get; set; } = null!;

        [Required]
        public DateTime Expires { get; set; }

        public string? DeviceBrowserInfo { get; set; }
        public string? IpAddress { get; set; }
        public DateTime? LastUsedDate { get; set; }

        public DateTime Created { get; set; } = DateTime.UtcNow;

        public DateTime? Revoked { get; set; }

        public bool IsExpired => DateTime.UtcNow >= Expires;
        public bool IsRevoked => Revoked != null;
        public bool IsActive => !IsRevoked && !IsExpired;
    }
}
