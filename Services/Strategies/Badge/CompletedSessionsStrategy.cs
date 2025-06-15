using ClimbUpAPI.Models;
using ClimbUpAPI.Models.Badges;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClimbUpAPI.Services.Strategies.Badge
{
    public class CompletedSessionsStrategy : IBadgeStrategy
    {
        public string MetricName => "completed_focus_sessions";

        public Task<BadgeEvaluationResult> Evaluate(UserStats stats, IReadOnlyCollection<BadgeLevel> levels)
        {
            long currentValue = stats.TotalCompletedSessions;
            BadgeLevel? eligibleLevel = null;

            foreach (var level in levels)
            {
                if (currentValue >= level.RequiredValue)
                {
                    eligibleLevel = level;
                }
                else
                {
                    break;
                }
            }

            var result = new BadgeEvaluationResult
            {
                CurrentValue = currentValue,
                EligibleLevel = eligibleLevel
            };

            return Task.FromResult(result);
        }
    }
}