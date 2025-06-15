using ClimbUpAPI.Models;
using ClimbUpAPI.Models.Badges;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClimbUpAPI.Services.Strategies.Badge
{
    public class BadgeEvaluationResult
    {
        public BadgeLevel? EligibleLevel { get; set; }
        public long CurrentValue { get; set; }
    }

    public interface IBadgeStrategy
    {
        string MetricName { get; }
        Task<BadgeEvaluationResult> Evaluate(UserStats stats, IReadOnlyCollection<BadgeLevel> levels);
    }
}