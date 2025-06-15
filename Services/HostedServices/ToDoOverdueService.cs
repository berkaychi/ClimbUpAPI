using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClimbUpAPI.Data;
using ClimbUpAPI.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClimbUpAPI.Services.HostedServices
{
    public class ToDoOverdueService : IHostedService, IDisposable
    {
        private readonly ILogger<ToDoOverdueService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private Timer? _timer;

        public ToDoOverdueService(ILogger<ToDoOverdueService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ToDo Overdue Service is starting.");

            var now = DateTime.UtcNow;
            var nextRunTime = now.Date.AddDays(1).AddHours(1);
            var dueTime = nextRunTime - now;

            if (dueTime.TotalMilliseconds <= 0)
            {
                dueTime = TimeSpan.FromHours(24) - (now - now.Date.AddHours(1));
                if (dueTime.TotalMilliseconds <= 0)
                {
                    nextRunTime = now.Date.AddDays(1).AddHours(1);
                    dueTime = nextRunTime - now;
                }
            }

            _logger.LogInformation("ToDo Overdue Service: Next check scheduled for {NextRunTimeUtc} (in {DueTime}).", nextRunTime, dueTime);

            _timer = new Timer(DoWork, null, dueTime, TimeSpan.FromHours(24));

            return Task.CompletedTask;
        }

        private async void DoWork(object? state)
        {
            _logger.LogInformation("ToDo Overdue Service is working. Timestamp: {Timestamp}", DateTime.UtcNow);

            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<ToDoOverdueService>>();

                try
                {
                    var today = DateTime.UtcNow.Date;
                    var overdueItems = await dbContext.ToDoItems
                        .Where(t => t.Status == ToDoStatus.Open && t.ForDate < today)
                        .ToListAsync();

                    if (overdueItems.Count != 0)
                    {
                        logger.LogInformation("Found {Count} ToDo items to mark as Overdue.", overdueItems.Count);
                        foreach (var item in overdueItems)
                        {
                            item.Status = ToDoStatus.Overdue;
                            logger.LogDebug("Marking ToDoItem {ToDoItemId} (ForDate: {ForDate}) as Overdue.", item.Id, item.ForDate);
                        }
                        await dbContext.SaveChangesAsync();
                        logger.LogInformation("Successfully marked {Count} ToDo items as Overdue.", overdueItems.Count);
                    }
                    else
                    {
                        logger.LogInformation("No ToDo items found to mark as Overdue at this time.");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while marking ToDo items as Overdue.");
                }
            }
            _logger.LogInformation("ToDo Overdue Service work finished. Next run in approximately 24 hours.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ToDo Overdue Service is stopping.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}