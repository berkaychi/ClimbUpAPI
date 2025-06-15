
namespace ClimbUpAPI.Models
{
    public class Tag
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public string Color { get; set; } = null!;

        public bool IsSystemDefined { get; set; } = false;

        public bool IsArchived { get; set; } = false;

        public string? UserId { get; set; }
        public virtual AppUser? User { get; set; }

        public virtual ICollection<FocusSessionTag> FocusSessionTags { get; set; } = [];
    }
}