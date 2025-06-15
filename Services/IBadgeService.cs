using ClimbUpAPI.Models.DTOs.BadgeDTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClimbUpAPI.Services
{
    public interface IBadgeService
    {
        Task<IEnumerable<BadgeDefinitionResponseDto>> GetBadgeDefinitionsAsync();
        Task<IEnumerable<UserBadgeResponseDto>> GetUserBadgesAsync(string userId);
        Task CheckAndAwardBadgesAsync(string userId);
        Task<IEnumerable<BadgeDefinitionResponseDto>> GetUserBadgesWithProgressAsync(string userId);
    }
}
