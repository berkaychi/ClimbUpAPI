namespace ClimbUpAPI.Models.DTOs.Admin.SessionTypeDTOs
{
    public class AdminSessionTypeResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public int WorkDuration { get; set; }
        public int? BreakDuration { get; set; }
        public int? NumberOfCycles { get; set; }
        public bool IsSystemDefined { get; set; }
        public bool IsActive { get; set; }
        public string? UserId { get; set; }
    }
}