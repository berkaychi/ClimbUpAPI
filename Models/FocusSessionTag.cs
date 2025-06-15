namespace ClimbUpAPI.Models
{
    public class FocusSessionTag
    {
        public int FocusSessionId { get; set; }
        public FocusSession FocusSession { get; set; } = null!;
        public int TagId { get; set; }
        public Tag Tag { get; set; } = null!;
    }
}