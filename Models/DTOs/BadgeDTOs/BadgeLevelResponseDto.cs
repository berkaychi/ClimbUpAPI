namespace ClimbUpAPI.Models.DTOs.BadgeDTOs
{
    public class BadgeLevelResponseDto
    {
        public int BadgeLevelID { get; set; }
        public int Level { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconURL { get; set; } = string.Empty;
        public int RequiredValue { get; set; }
    }
}
