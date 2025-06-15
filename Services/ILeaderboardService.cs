using ClimbUpAPI.Models.DTOs;
using System.Threading.Tasks;

namespace ClimbUpAPI.Services
{
    public interface ILeaderboardService
    {
        Task<LeaderboardResponseDto> GetLeaderboardAsync(LeaderboardQueryParametersDto queryParameters);
    }
}