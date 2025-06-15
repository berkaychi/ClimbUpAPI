namespace ClimbUpAPI.Models.DTOs.TagDTOs
{
    public class TagDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Color { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsSystemDefined { get; set; }
        public bool IsArchived { get; set; }
    }

}