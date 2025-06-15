using ClimbUpAPI.Models.Enums;

namespace ClimbUpAPI.Models.DTOs.TaskDTOs
{
    public class AppTaskDto
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; }
        public TaskType TaskType { get; set; }
        public int TargetProgress { get; set; }
        public string? Recurrence { get; set; }
    }
}