using AutoMapper;
using ClimbUpAPI.Data;
using ClimbUpAPI.Models;
using ClimbUpAPI.Models.Badges;
using ClimbUpAPI.Models.DTOs.BadgeDTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClimbUpAPI.Services.Strategies.Badge;

namespace ClimbUpAPI.Services.Implementations
{
    public class BadgeService : IBadgeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<BadgeService> _logger;
        private readonly IReadOnlyDictionary<string, IBadgeStrategy> _badgeStrategies;

        public BadgeService(ApplicationDbContext context, IMapper mapper, ILogger<BadgeService> logger, IEnumerable<IBadgeStrategy> badgeStrategies)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _badgeStrategies = badgeStrategies.ToDictionary(s => s.MetricName, s => s);
        }

        public async Task<IEnumerable<BadgeDefinitionResponseDto>> GetBadgeDefinitionsAsync()
        {
            _logger.LogInformation("Fetching all badge definitions with their levels.");
            var definitions = await _context.BadgeDefinitions
                .Include(bd => bd.BadgeLevels.OrderBy(bl => bl.Level))
                .ToListAsync();
            return _mapper.Map<IEnumerable<BadgeDefinitionResponseDto>>(definitions);
        }

        public async Task<IEnumerable<UserBadgeResponseDto>> GetUserBadgesAsync(string userId)
        {
            _logger.LogInformation("Fetching badges for User {UserId}.", userId);
            var userBadges = await _context.UserBadges
                .Where(ub => ub.UserId == userId)
                .Include(ub => ub.BadgeLevel)
                    .ThenInclude(bl => bl.BadgeDefinition)
                .OrderByDescending(ub => ub.DateAchieved)
                .ToListAsync();

            return _mapper.Map<IEnumerable<UserBadgeResponseDto>>(userBadges);
        }

        public async Task CheckAndAwardBadgesAsync(string userId)
        {
            _logger.LogInformation("Checking and awarding badges for User {UserId}.", userId);
            var userStats = await _context.UserStats.FirstOrDefaultAsync(us => us.UserId == userId);
            if (userStats == null)
            {
                _logger.LogWarning("UserStats not found for User {UserId}. Cannot check badges.", userId);
                return;
            }

            var allBadgeDefinitions = await _context.BadgeDefinitions
                .Include(bd => bd.BadgeLevels.OrderBy(bl => bl.Level))
                .ToListAsync();

            var userExistingBadges = await _context.UserBadges
                .Where(ub => ub.UserId == userId)
                .Include(ub => ub.BadgeLevel)
                .ToListAsync();

            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (appUser == null)
            {
                _logger.LogError("AppUser not found for UserId {UserId} during CheckAndAwardBadgesAsync. Cannot award Steps/Stepstones for badges.", userId);
            }

            bool newBadgeAwardedOrUpdated = false;

            foreach (var definition in allBadgeDefinitions)
            {
                if (!_badgeStrategies.TryGetValue(definition.MetricToTrack, out var strategy))
                {
                    _logger.LogWarning("Unknown metric to track or no strategy found for: {MetricToTrack} for BadgeDefinition {BadgeDefinitionId}", definition.MetricToTrack, definition.BadgeDefinitionID);
                    continue;
                }

                var evaluationResult = await strategy.Evaluate(userStats, definition.BadgeLevels.ToList());
                var eligibleLevel = evaluationResult.EligibleLevel;
                var currentValue = evaluationResult.CurrentValue;

                if (eligibleLevel != null)
                {
                    var existingUserBadge = userExistingBadges
                        .FirstOrDefault(ub => ub.BadgeLevel.BadgeDefinitionID == definition.BadgeDefinitionID);

                    if (existingUserBadge == null)
                    {
                        _context.UserBadges.Add(new UserBadge
                        {
                            UserId = userId,
                            BadgeLevelID = eligibleLevel.BadgeLevelID,
                            DateAchieved = DateTime.UtcNow
                        });
                        newBadgeAwardedOrUpdated = true;
                        _logger.LogInformation("User {UserId} awarded new badge: {BadgeName} (Level {Level}) for metric {Metric} with value {Value}",
                            userId, eligibleLevel.Name, eligibleLevel.Level, definition.MetricToTrack, currentValue);

                        if (appUser != null)
                        {
                            AwardStepsForBadgeLevel(appUser, eligibleLevel, definition.CoreName);
                        }
                    }
                    else if (existingUserBadge.BadgeLevelID != eligibleLevel.BadgeLevelID && existingUserBadge.BadgeLevel.Level < eligibleLevel.Level)
                    {
                        existingUserBadge.BadgeLevelID = eligibleLevel.BadgeLevelID;
                        existingUserBadge.DateAchieved = DateTime.UtcNow;
                        _context.UserBadges.Update(existingUserBadge);
                        newBadgeAwardedOrUpdated = true;
                        _logger.LogInformation("User {UserId} upgraded badge: {BadgeName} to Level {Level} (from Level {OldLevel}) for metric {Metric} with value {Value}",
                            userId, eligibleLevel.Name, eligibleLevel.Level, existingUserBadge.BadgeLevel.Level, definition.MetricToTrack, currentValue);

                        if (appUser != null)
                        {
                            AwardStepsForBadgeLevel(appUser, eligibleLevel, definition.CoreName);
                        }
                    }
                }
            }

            if (newBadgeAwardedOrUpdated)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Saved badge changes for User {UserId}.", userId);
            }
            else
            {
                _logger.LogInformation("No new badges or upgrades for User {UserId} at this time.", userId);
            }
        }

        private void AwardStepsForBadgeLevel(AppUser appUser, BadgeLevel badgeLevel, string badgeCoreName)
        {
            long stepsToAward = badgeLevel.AwardedSteps;

            if (stepsToAward > 0)
            {
                long stepstonesToAward = stepsToAward / 10;
                appUser.TotalSteps += stepsToAward;
                appUser.Stepstones += stepstonesToAward;
                _logger.LogInformation("User {UserId} earned {Steps} Steps and {Stepstones} Stepstones for achieving badge: {BadgeName} (Level {BadgeLevel}) - {BadgeCoreName}",
                    appUser.Id, stepsToAward, stepstonesToAward, badgeLevel.Name, badgeLevel.Level, badgeCoreName);
            }
        }
        public async Task<IEnumerable<BadgeDefinitionResponseDto>> GetUserBadgesWithProgressAsync(string userId)
        {
            _logger.LogInformation("Fetching badge definitions with progress for User {UserId}.", userId);

            var userStats = await _context.UserStats.AsNoTracking().FirstOrDefaultAsync(us => us.UserId == userId);
            if (userStats == null)
            {
                _logger.LogWarning("UserStats not found for User {UserId}. Cannot determine badge progress.", userId);
                return new List<BadgeDefinitionResponseDto>();
            }

            var allBadgeDefinitions = await _context.BadgeDefinitions
                .AsNoTracking()
                .Include(bd => bd.BadgeLevels.OrderBy(bl => bl.Level))
                .ToListAsync();

            var userAchievedBadgesLookup = await _context.UserBadges
                .AsNoTracking()
                .Where(ub => ub.UserId == userId)
                .Include(ub => ub.BadgeLevel)
                .ToDictionaryAsync(ub => ub.BadgeLevel.BadgeDefinitionID, ub => ub.BadgeLevel);

            var resultList = new List<BadgeDefinitionResponseDto>();

            foreach (var definition in allBadgeDefinitions)
            {
                var dto = _mapper.Map<BadgeDefinitionResponseDto>(definition);

                if (_badgeStrategies.TryGetValue(definition.MetricToTrack, out var strategy))
                {
                    var evaluationResult = await strategy.Evaluate(userStats, definition.BadgeLevels.ToList());
                    dto.CurrentUserProgress = evaluationResult.CurrentValue;
                }
                else
                {
                    _logger.LogWarning("No strategy found for metric: {MetricToTrack} while getting progress for BadgeDefinition {BadgeDefinitionId}.", definition.MetricToTrack, definition.BadgeDefinitionID);
                    dto.CurrentUserProgress = 0;
                }

                BadgeLevel? currentAchievedLevel = null;
                if (userAchievedBadgesLookup.TryGetValue(definition.BadgeDefinitionID, out var achievedLevel))
                {
                    currentAchievedLevel = achievedLevel;
                    dto.CurrentAchievedLevel = achievedLevel.Level;
                }

                var nextUnachievedLevel = definition.BadgeLevels
                    .FirstOrDefault(level => level.Level > (currentAchievedLevel?.Level ?? 0));

                if (nextUnachievedLevel != null)
                {
                    dto.NextLevelRequiredValue = nextUnachievedLevel.RequiredValue;
                    dto.IsMaxLevelAchieved = false;
                }
                else
                {
                    dto.NextLevelRequiredValue = null;
                    var maxLevelInDefinition = definition.BadgeLevels.Max(bl => (int?)bl.Level);
                    dto.IsMaxLevelAchieved = currentAchievedLevel != null && currentAchievedLevel.Level == maxLevelInDefinition;
                }

                if (definition.BadgeLevels.Count == 0)
                {
                    dto.IsMaxLevelAchieved = true;
                    dto.NextLevelRequiredValue = null;
                }


                resultList.Add(dto);
            }

            return resultList;
        }
    }
}
