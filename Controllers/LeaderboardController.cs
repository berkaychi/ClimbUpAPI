using ClimbUpAPI.Models.DTOs;
using ClimbUpAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ClimbUpAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeaderboardController : ControllerBase
    {
        private readonly ILeaderboardService _leaderboardService;

        public LeaderboardController(ILeaderboardService leaderboardService)
        {
            _leaderboardService = leaderboardService;
        }

        [HttpGet]
        public async Task<ActionResult<LeaderboardResponseDto>> GetLeaderboard([FromQuery] LeaderboardQueryParametersDto queryParameters)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var leaderboard = await _leaderboardService.GetLeaderboardAsync(queryParameters);
                return Ok(leaderboard);
            }
            catch (System.ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}