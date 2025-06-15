using System.ComponentModel.DataAnnotations;
using ClimbUpAPI.Models.Enums;

namespace ClimbUpAPI.Models.DTOs
{
    public class LeaderboardQueryParametersDto
    {
        public LeaderboardSortCriteria SortBy { get; set; } = LeaderboardSortCriteria.TotalFocusDuration;

        public LeaderboardPeriod Period { get; set; } = LeaderboardPeriod.AllTime;

        [Range(1, 100, ErrorMessage = "Limit must be between 1 and 100.")]
        public int Limit { get; set; } = 10;

        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0.")]
        public int Page { get; set; } = 1;
    }
}