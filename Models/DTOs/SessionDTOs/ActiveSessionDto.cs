namespace ClimbUpAPI.Models.DTOs.SessionDTOs
{
    public class ActiveSessionDto
    {
        public int Id { get; set; }
        public string? DeviceBrowserInfo { get; set; }
        public string? IpAddress { get; set; }
        public DateTime Created { get; set; }
        public DateTime Expires { get; set; }
        public DateTime? LastUsedDate { get; set; }
        public bool IsCurrentSession { get; set; }
    }
}