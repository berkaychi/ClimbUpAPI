namespace ClimbUpAPI.Models.DTOs.TagDTOs
{
    public class CreateTagDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string Color { get; set; } = "#465956";
    }
}