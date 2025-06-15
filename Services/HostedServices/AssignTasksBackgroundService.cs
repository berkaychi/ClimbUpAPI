using ClimbUpAPI.Data;
using ClimbUpAPI.Models;
using ClimbUpAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ClimbUpAPI.Services.HostedServices
{
    public class AssignTasksBackgroundService : BackgroundService
    {
        private readonly ILogger<AssignTasksBackgroundService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public AssignTasksBackgroundService(ILogger<AssignTasksBackgroundService> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AssignTasksBackgroundService is starting.");

            stoppingToken.Register(() => _logger.LogInformation("AssignTasksBackgroundService is stopping."));

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("AssignTasksBackgroundService is working. Attempting to assign/refresh tasks for users.");

                try
                {
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();
                        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        var users = await userManager.Users.ToListAsync(stoppingToken);

                        _logger.LogInformation("Fetching active AppTask definitions once for this cycle.");
                        List<AppTask> activeAppTaskDefinitions;
                        try
                        {
                            activeAppTaskDefinitions = await dbContext.AppTasks
                                                                    .Where(t => t.IsActive)
                                                                    .ToListAsync(stoppingToken);
                            _logger.LogInformation($"Successfully fetched {activeAppTaskDefinitions.Count} active AppTask definitions.");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error fetching active AppTask definitions in AssignTasksBackgroundService.");
                            activeAppTaskDefinitions = new List<AppTask>();
                        }

                        if (!users.Any())
                        {
                            _logger.LogInformation("No users found to process for task assignment.");
                        }
                        else
                        {
                            _logger.LogInformation($"Found {users.Count} users to process.");
                            if (!activeAppTaskDefinitions.Any())
                            {
                                _logger.LogWarning("No active AppTask definitions found, but users exist. No tasks will be assigned or refreshed in this cycle.");
                            }
                        }

                        if (activeAppTaskDefinitions.Any() && users.Any())
                        {
                            foreach (var user in users)
                            {
                                if (stoppingToken.IsCancellationRequested)
                                {
                                    _logger.LogInformation("Cancellation requested during user loop, stopping task assignment for user {UserId}.", user.Id);
                                    break;
                                }

                                try
                                {
                                    _logger.LogInformation("Assigning/refreshing tasks for user {UserId} - {UserName}.", user.Id, user.UserName);
                                    await taskService.AssignOrRefreshTasksAsync(user.Id, activeAppTaskDefinitions);
                                    _logger.LogInformation("Successfully assigned/refreshed tasks for user {UserId} - {UserName}.", user.Id, user.UserName);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error assigning/refreshing tasks for user {UserId} - {UserName}.", user.Id, user.UserName);
                                }
                            }
                        }
                        else if (users.Any())
                        {
                            _logger.LogInformation("Skipping task assignment for users as no active AppTask definitions were found.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred in AssignTasksBackgroundService main processing loop.");
                }

                try
                {
                    _logger.LogInformation("AssignTasksBackgroundService waiting for 1 hour before next run.");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Task.Delay was canceled, AssignTasksBackgroundService is stopping.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during Task.Delay in AssignTasksBackgroundService. Service will attempt to continue on next cycle.");
                }
            }

            _logger.LogInformation("AssignTasksBackgroundService has stopped.");
        }
    }
}