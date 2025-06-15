namespace ClimbUpAPI.Models.DTOs.SessionDTOs
{
    public class SessionTypeResponseDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        public int WorkDuration { get; set; }
        public int? BreakDuration { get; set; }
        public int? NumberOfCycles { get; set; }
        public bool IsSystemDefined { get; set; }
        public bool IsActive { get; set; }
    }

}