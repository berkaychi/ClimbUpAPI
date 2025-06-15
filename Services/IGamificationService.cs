using ClimbUpAPI.Models;
using ClimbUpAPI.Models.DTOs.Gamification;
using System.Threading.Tasks;

namespace ClimbUpAPI.Services
{
    public interface IGamificationService
    {
        Task<AwardedPointsDto?> AwardStepsForSessionCompletionAsync(FocusSession completedSession, AppUser user, UserStats userStats);

        Task<AwardedPointsDto?> AwardStepsForNewCustomTagUsageAsync(string userId, int tagId, AppUser user);

        Task<AwardedPointsDto?> AwardStepsForNewCustomSessionTypeUsageAsync(string userId, int sessionTypeId, AppUser user);

        Task<AwardedPointsDto?> AwardStepsForToDoCompletionWithFocusAsync(string userId, int toDoItemId, AppUser user);
    }
}