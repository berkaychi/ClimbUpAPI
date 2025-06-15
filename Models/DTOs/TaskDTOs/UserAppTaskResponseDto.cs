using ClimbUpAPI.Models.Enums;

namespace ClimbUpAPI.Models.DTOs.TaskDTOs
{
    public class UserAppTaskResponseDto
    {
        public int Id { get; set; }
        public required AppTaskDto AppTaskDefinition { get; set; }
        public DateTime AssignedDate { get; set; }
        public DateTime? DueDate { get; set; }
        public int CurrentProgress { get; set; }
        public ClimbUpAPI.Models.Enums.TaskStatus Status { get; set; }
        public DateTime? CompletedDate { get; set; }
    }
}