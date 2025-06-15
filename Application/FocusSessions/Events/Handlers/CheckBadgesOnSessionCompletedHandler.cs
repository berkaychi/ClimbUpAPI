using ClimbUpAPI.Services;
using MediatR;

namespace ClimbUpAPI.Application.FocusSessions.Events.Handlers
{
    public class CheckBadgesOnSessionCompletedHandler : INotificationHandler<FocusSessionCompletedNotification>
    {
        private readonly IBadgeService _badgeService;
        private readonly ILogger<CheckBadgesOnSessionCompletedHandler> _logger;

        public CheckBadgesOnSessionCompletedHandler(IBadgeService badgeService, ILogger<CheckBadgesOnSessionCompletedHandler> logger)
        {
            _badgeService = badgeService;
            _logger = logger;
        }

        public async Task Handle(FocusSessionCompletedNotification notification, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Handling FocusSessionCompletedNotification: Checking badges for User {UserId} from session {SessionId}",
                    notification.CompletedSession.UserId, notification.CompletedSession.Id);

                await _badgeService.CheckAndAwardBadgesAsync(notification.CompletedSession.UserId);

                _logger.LogInformation("Badge check completed for User {UserId} triggered by session {SessionId}",
                    notification.CompletedSession.UserId, notification.CompletedSession.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking badges for User {UserId} after session {SessionId} completion.",
                    notification.CompletedSession.UserId, notification.CompletedSession.Id);
            }
        }
    }
}