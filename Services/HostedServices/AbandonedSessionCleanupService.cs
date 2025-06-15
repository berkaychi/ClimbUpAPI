using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClimbUpAPI.Data;
using ClimbUpAPI.Models;
using ClimbUpAPI.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClimbUpAPI.Services.HostedServices
{
    public class AbandonedSessionCleanupService : IHostedService, IDisposable
    {
        private readonly ILogger<AbandonedSessionCleanupService> _logger;
        private Timer? _timer;
        private readonly IServiceProvider _serviceProvider;

        public AbandonedSessionCleanupService(ILogger<AbandonedSessionCleanupService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Abandoned Session Cleanup Service is starting.");
            _timer = new Timer(DoWork, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
            return Task.CompletedTask;
        }

        private async void DoWork(object? state)
        {
            _logger.LogInformation("Abandoned Session Cleanup Service is working. Timestamp: {Timestamp}", DateTime.UtcNow);

            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var abandonmentThreshold = DateTime.UtcNow.AddMinutes(-10);

                var abandonedSessions = await dbContext.FocusSessions
                    .Where(s => (s.Status == SessionState.Working || s.Status == SessionState.Break) && s.CurrentStateEndTime < abandonmentThreshold)
                    .Include(s => s.ToDoItem)
                    .ToListAsync();

                if (abandonedSessions.Any())
                {
                    _logger.LogInformation("Found {Count} abandoned sessions to cancel.", abandonedSessions.Count);
                    var today = DateTime.UtcNow.Date;

                    foreach (var session in abandonedSessions)
                    {
                        session.Status = SessionState.Cancelled;
                        session.EndTime = DateTime.UtcNow;
                        _logger.LogInformation("FocusSession {SessionId} cancelled due to abandonment. EndTime set to {EndTime}.", session.Id, session.EndTime);


                        if (session.ToDoItemId.HasValue && session.ToDoItem != null && session.ToDoItem.Status != ToDoStatus.Completed)
                        {
                            if (session.ToDoItem.ForDate < today)
                            {
                                session.ToDoItem.Status = ToDoStatus.Overdue;
                                _logger.LogInformation("ToDoItem {ToDoItemId} (ForDate: {ForDate}) status updated to Overdue due to abandoned session {SessionId}.",
                                    session.ToDoItemId.Value, session.ToDoItem.ForDate, session.Id);
                            }
                            else
                            {
                                session.ToDoItem.Status = ToDoStatus.Open;
                                _logger.LogInformation("ToDoItem {ToDoItemId} (ForDate: {ForDate}) status updated to Open due to abandoned session {SessionId}.",
                                    session.ToDoItemId.Value, session.ToDoItem.ForDate, session.Id);
                            }
                        }
                    }
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Successfully processed {Count} abandoned sessions.", abandonedSessions.Count);
                }
                else
                {
                    _logger.LogInformation("No abandoned sessions found requiring cleanup.");
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Abandoned Session Cleanup Service is stopping.");
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