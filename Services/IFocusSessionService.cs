using ClimbUpAPI.Models.DTOs;
using ClimbUpAPI.Models.DTOs.Gamification;
using ClimbUpAPI.Models.DTOs.SessionDTOs;

namespace ClimbUpAPI.Services
{
    public interface IFocusSessionService
    {
        Task UpdateAsync(int id, UpdateFocusSessionDto dto, string userId);
        Task<AwardedPointsDto?> UpdateStatusAsync(int id, UpdateFocusSessionStatusDto dto, string userId);
        Task<FocusSessionResponseDto?> GetByIdAsync(int id, string userId);
        Task<List<FocusSessionResponseDto>> GetUserSessionsAsync(string userId);
        Task<FocusSessionResponseDto?> GetOngoingSessionAsync(string userId);
        Task<GamifiedResponse<FocusSessionResponseDto>> CreateFocusSessionAsync(CreateFocusSessionDto dto, string userId);
        Task<GamifiedResponse<FocusSessionResponseDto>> TransitionSessionStateAsync(int sessionId, string userId);
    }

}