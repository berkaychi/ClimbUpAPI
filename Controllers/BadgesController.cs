using ClimbUpAPI.Data;
using ClimbUpAPI.Models.DTOs.BadgeDTOs;
using ClimbUpAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ClimbUpAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BadgesController : ControllerBase
    {
        private readonly IBadgeService _badgeService;
        private readonly ILogger<BadgesController> _logger;
        private readonly ApplicationDbContext _context;

        public BadgesController(IBadgeService badgeService, ILogger<BadgesController> logger, ApplicationDbContext context)
        {
            _badgeService = badgeService;
            _logger = logger;
            _context = context;
        }

        [HttpGet("definitions")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<BadgeDefinitionResponseDto>>> GetBadgeDefinitions()
        {
            _logger.LogInformation("Attempting to retrieve all badge definitions.");
            var definitions = await _badgeService.GetBadgeDefinitionsAsync();
            _logger.LogInformation("Successfully retrieved {Count} badge definitions.", definitions.Count());
            return Ok(definitions);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<UserBadgeResponseDto>>> GetMyBadges()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID not found in claims for GetMyBadges.");
                return Unauthorized("User ID not found in token.");
            }

            _logger.LogInformation("Attempting to retrieve badges for current user {UserId}.", userId);
            var userBadges = await _badgeService.GetUserBadgesAsync(userId);
            _logger.LogInformation("Successfully retrieved {Count} badges for user {UserId}.", userBadges.Count(), userId);
            return Ok(userBadges);
        }

        [HttpGet("me/progress")]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<BadgeDefinitionResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetUserBadgesWithProgress()
        {
            var requestingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(requestingUserId))
            {
                _logger.LogWarning("User ID not found in claims for GetUserBadgesWithProgress.");
                return Unauthorized("User ID not found in token.");
            }

            _logger.LogInformation("Attempting to retrieve badge progress for current user {UserId}.", requestingUserId);

            var badgeProgress = await _badgeService.GetUserBadgesWithProgressAsync(requestingUserId);
            _logger.LogInformation("Successfully retrieved {Count} badge definitions with progress for user {UserId}.", badgeProgress.Count(), requestingUserId);
            return Ok(badgeProgress);
        }
    }
}
