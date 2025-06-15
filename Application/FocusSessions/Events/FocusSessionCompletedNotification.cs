using ClimbUpAPI.Models;
using MediatR;

namespace ClimbUpAPI.Application.FocusSessions.Events
{
    public class FocusSessionCompletedNotification : INotification
    {
        public required FocusSession CompletedSession { get; init; }
    }
}