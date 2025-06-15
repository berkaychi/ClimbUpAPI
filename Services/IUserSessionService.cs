using ClimbUpAPI.Models.DTOs.SessionDTOs;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClimbUpAPI.Services
{
    public interface IUserSessionService
    {
        Task<List<ActiveSessionDto>> GetActiveSessionsAsync(string userId, string? currentTokenValue = null);
        Task<IdentityResult> RevokeSessionAsync(string userId, int refreshTokenId);
        Task<IdentityResult> RevokeAllOtherSessionsAsync(string userId, string currentTokenValue);
    }
}