using System.Collections.Generic;

namespace ClimbUpAPI.Models.DTOs.TaskDTOs
{
    public class CurrentUserTasksDto
    {
        public List<UserAppTaskResponseDto> DailyTasks { get; set; } = new List<UserAppTaskResponseDto>();
        public List<UserAppTaskResponseDto> WeeklyTasks { get; set; } = new List<UserAppTaskResponseDto>();
    }
}