using ClimbUpAPI.Data;
using ClimbUpAPI.Models;
using ClimbUpAPI.Models.Enums;
using ClimbUpAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using AutoMapper;
using ClimbUpAPI.Models.DTOs.TaskDTOs;

namespace ClimbUpAPI.Services.Implementations
{
    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ILogger<TaskService> _logger;

        public TaskService(ApplicationDbContext context, UserManager<AppUser> userManager, IMapper mapper, ILogger<TaskService> logger)
        {
            _context = context;
            _userManager = userManager;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task AssignOrRefreshTasksAsync(string userId, IEnumerable<AppTask> activeAppTasks)
        {
            var userTasks = await _context.UserAppTasks
                                          .Where(ut => ut.AppUserId == userId)
                                          .ToListAsync();

            var newTasksToAdd = new List<UserAppTask>();

            foreach (var appTask in activeAppTasks)
            {
                switch (appTask.TaskType)
                {
                    case TaskType.DailyFocusDuration:
                        var existingDailyTask = userTasks.FirstOrDefault(ut =>
                            ut.AppTaskId == appTask.Id &&
                            ut.AssignedDate.Date == DateTime.UtcNow.Date &&
                            (ut.Status == Models.Enums.TaskStatus.Pending || ut.Status == Models.Enums.TaskStatus.InProgress));

                        if (existingDailyTask == null)
                        {

                            var anyTaskForToday = userTasks.Any(ut =>
                                ut.AppTaskId == appTask.Id &&
                                ut.AssignedDate.Date == DateTime.UtcNow.Date);

                            if (!anyTaskForToday)
                            {
                                var user = await _context.Users.FindAsync(userId);
                                if (user == null)
                                {
                                    _logger.LogWarning("User with ID {UserId} not found while trying to assign recurring task {AppTaskId}.", userId, appTask.Id);
                                    continue;
                                }
                                newTasksToAdd.Add(new UserAppTask
                                {
                                    AppUserId = userId,
                                    AppUser = user,
                                    AppTaskId = appTask.Id,
                                    AppTaskDefinition = appTask,
                                    AssignedDate = DateTime.UtcNow,
                                    DueDate = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1),
                                    Status = Models.Enums.TaskStatus.Pending
                                });
                            }
                        }
                        break;

                    case TaskType.WeeklyFocusDuration:
                        DayOfWeek currentDay = DateTime.UtcNow.DayOfWeek;
                        int daysToSubtract = (currentDay == DayOfWeek.Sunday) ? 6 : (int)currentDay - (int)DayOfWeek.Monday;
                        DateTime startOfWeek = DateTime.UtcNow.Date.AddDays(-daysToSubtract);
                        DateTime endOfWeek = startOfWeek.AddDays(7).AddTicks(-1);

                        var existingWeeklyTask = userTasks.FirstOrDefault(ut =>
                            ut.AppTaskId == appTask.Id &&
                            ut.DueDate >= startOfWeek &&
                            ut.DueDate <= endOfWeek &&
                            (ut.Status == Models.Enums.TaskStatus.Pending || ut.Status == Models.Enums.TaskStatus.InProgress));

                        if (existingWeeklyTask == null)
                        {
                            var anyTaskForThisMonSunWeek = userTasks.Any(ut =>
                               ut.AppTaskId == appTask.Id &&
                               ut.DueDate >= startOfWeek && ut.DueDate <= endOfWeek);

                            if (!anyTaskForThisMonSunWeek)
                            {
                                var user = await _context.Users.FindAsync(userId);
                                if (user == null)
                                {
                                    _logger.LogWarning("User with ID {UserId} not found while trying to assign recurring weekly task {AppTaskId}.", userId, appTask.Id);
                                    continue;
                                }
                                newTasksToAdd.Add(new UserAppTask
                                {
                                    AppUserId = userId,
                                    AppUser = user,
                                    AppTaskId = appTask.Id,
                                    AppTaskDefinition = appTask,
                                    AssignedDate = DateTime.UtcNow,
                                    DueDate = endOfWeek,
                                    Status = Models.Enums.TaskStatus.Pending
                                });
                            }
                        }
                        break;
                }
            }

            if (newTasksToAdd.Any())
            {
                _context.UserAppTasks.AddRange(newTasksToAdd);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateTaskProgressAsync(string userId, TaskType taskType, int durationToAddInMinutes)
        {
            var today = DateTime.UtcNow.Date;
            DayOfWeek currentDayForUpdate = today.DayOfWeek;
            int daysToSubtractForUpdate = (currentDayForUpdate == DayOfWeek.Sunday) ? 6 : (int)currentDayForUpdate - (int)DayOfWeek.Monday;
            var startOfWeek = today.AddDays(-daysToSubtractForUpdate);
            var endOfWeek = startOfWeek.AddDays(7).AddTicks(-1);

            var activeUserTaskQuery = _context.UserAppTasks
                .Include(uat => uat.AppTaskDefinition)
                .Where(uat => uat.AppUserId == userId &&
                              uat.AppTaskDefinition.TaskType == taskType &&
                              (uat.Status == Models.Enums.TaskStatus.Pending || uat.Status == Models.Enums.TaskStatus.InProgress));

            UserAppTask? userTaskToUpdate = null;

            if (taskType == TaskType.DailyFocusDuration)
            {
                userTaskToUpdate = await activeUserTaskQuery
                    .Where(uat => uat.AssignedDate.Date == today)
                    .FirstOrDefaultAsync();
            }
            else if (taskType == TaskType.WeeklyFocusDuration)
            {
                userTaskToUpdate = await activeUserTaskQuery
                   .Where(uat => uat.DueDate >= startOfWeek && uat.DueDate <= endOfWeek)
                   .FirstOrDefaultAsync();
            }

            if (userTaskToUpdate != null)
            {
                userTaskToUpdate.CurrentProgress += durationToAddInMinutes;
                userTaskToUpdate.Status = Models.Enums.TaskStatus.InProgress;

                if (userTaskToUpdate.CurrentProgress >= userTaskToUpdate.AppTaskDefinition.TargetProgress)
                {
                    userTaskToUpdate.CurrentProgress = userTaskToUpdate.AppTaskDefinition.TargetProgress;
                    userTaskToUpdate.Status = Models.Enums.TaskStatus.Completed;
                    userTaskToUpdate.CompletedDate = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
            }
        }
        public async Task<Models.DTOs.TaskDTOs.CurrentUserTasksDto> GetCurrentUserTasksAsync(string userId)
        {
            var today = DateTime.UtcNow.Date;
            DayOfWeek currentDayForGet = today.DayOfWeek;
            int daysToSubtractForGet = (currentDayForGet == DayOfWeek.Sunday) ? 6 : (int)currentDayForGet - (int)DayOfWeek.Monday;
            var startOfWeek = today.AddDays(-daysToSubtractForGet);
            var endOfWeek = startOfWeek.AddDays(7).AddTicks(-1);

            var userTasks = await _context.UserAppTasks
                .Include(uat => uat.AppTaskDefinition)
                .Where(uat => uat.AppUserId == userId &&
                              (uat.Status == Models.Enums.TaskStatus.Pending || uat.Status == Models.Enums.TaskStatus.InProgress))
                .ToListAsync();

            var allTaskDtos = _mapper.Map<List<UserAppTaskResponseDto>>(userTasks);
            var result = new CurrentUserTasksDto();

            foreach (var taskDto in allTaskDtos)
            {
                if (taskDto.AppTaskDefinition.TaskType == TaskType.DailyFocusDuration && taskDto.AssignedDate.Date == today)
                {
                    result.DailyTasks.Add(taskDto);
                }
                else if (taskDto.AppTaskDefinition.TaskType == TaskType.WeeklyFocusDuration && taskDto.DueDate >= startOfWeek && taskDto.DueDate <= endOfWeek)
                {
                    result.WeeklyTasks.Add(taskDto);
                }
            }
            return result;
        }
    }
}
