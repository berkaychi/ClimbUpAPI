using ClimbUpAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using ClimbUpAPI.Models.DTOs.TaskDTOs;

namespace ClimbUpAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TaskController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        [HttpGet("my-current")]
        public async Task<ActionResult<CurrentUserTasksDto>> GetMyCurrentTasks()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var tasks = await _taskService.GetCurrentUserTasksAsync(userId);
            return Ok(tasks);
        }
    }
}