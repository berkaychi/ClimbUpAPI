using Microsoft.AspNetCore.Identity;

namespace ClimbUpAPI.Models
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; } = null!;
        public DateTime DateAdded { get; set; }
        public string? ProfilePictureUrl { get; set; }

        public string? EmailConfirmationCode { get; set; }
        public DateTime? EmailConfirmationCodeExpiration { get; set; }

        public string? PendingNewEmail { get; set; }
        public string? PendingEmailChangeToken { get; set; }
        public DateTime? PendingEmailChangeTokenExpiration { get; set; }

        public virtual ICollection<Tag> Tags { get; set; } = [];
        public virtual ICollection<SessionType> SessionTypes { get; set; } = [];

        public virtual ICollection<UserAppTask> UserAppTasks { get; set; } = new List<UserAppTask>();
        public virtual UserStats? UserStats { get; set; }

        public long TotalSteps { get; set; } = 0;
        public long Stepstones { get; set; } = 0;

        public bool IsCompassActive { get; set; } = false;
        public bool IsEnergyBarActiveForNextSession { get; set; } = false;

        public virtual ICollection<UserStoreItem> UserStoreItems { get; set; } = new List<UserStoreItem>();
    }
}
