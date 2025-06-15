using ClimbUpAPI.Services;
using MediatR;

namespace ClimbUpAPI.Application.FocusSessions.Events.Handlers
{
    public class UpdateTasksOnSessionCompletedHandler : INotificationHandler<FocusSessionCompletedNotification>
    {
        private readonly ITaskService _taskService;
        private readonly ILogger<UpdateTasksOnSessionCompletedHandler> _logger;

        public UpdateTasksOnSessionCompletedHandler(ITaskService taskService, ILogger<UpdateTasksOnSessionCompletedHandler> logger)
        {
            _taskService = taskService;
            _logger = logger;
        }

        public async Task Handle(FocusSessionCompletedNotification notification, CancellationToken cancellationToken)
        {
            try
            {
                var session = notification.CompletedSession;
                int durationInMinutes = 0;

                if (session.EndTime.HasValue && session.StartTime != default)
                {
                    durationInMinutes = (int)(session.EndTime.Value - session.StartTime).TotalMinutes;
                }

                if (durationInMinutes > 0)
                {
                    _logger.LogInformation("Handling FocusSessionCompletedNotification: Updating task progress for User {UserId} from session {SessionId} with duration {DurationMinutes}m.",
                        session.UserId, session.Id, durationInMinutes);

                    await _taskService.UpdateTaskProgressAsync(session.UserId, Models.Enums.TaskType.DailyFocusDuration, durationInMinutes);
                    await _taskService.UpdateTaskProgressAsync(session.UserId, Models.Enums.TaskType.WeeklyFocusDuration, durationInMinutes);

                    _logger.LogInformation("Task progress update completed for User {UserId} triggered by session {SessionId}.",
                        session.UserId, session.Id);
                }
                else
                {
                    _logger.LogWarning("Session {SessionId} completed for User {UserId}, but duration was zero or negative. No task progress updated.",
                        session.Id, session.UserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task progress for User {UserId} after session {SessionId} completion.",
                    notification.CompletedSession.UserId, notification.CompletedSession.Id);
            }
        }
    }
}