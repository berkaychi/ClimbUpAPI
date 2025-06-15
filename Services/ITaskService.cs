using ClimbUpAPI.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using ClimbUpAPI.Models.Enums;
using ClimbUpAPI.Models.DTOs.TaskDTOs;

namespace ClimbUpAPI.Services
{
    public interface ITaskService
    {
        Task AssignOrRefreshTasksAsync(string userId, IEnumerable<AppTask> activeAppTasks);
        Task UpdateTaskProgressAsync(string userId, TaskType taskType, int durationToAddInMinutes);
        Task<CurrentUserTasksDto> GetCurrentUserTasksAsync(string userId);
    }
}