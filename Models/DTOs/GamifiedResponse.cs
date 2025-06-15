using ClimbUpAPI.Models.DTOs.Gamification;

namespace ClimbUpAPI.Models.DTOs
{
    public class GamifiedResponse<T>
    {
        public required T Data { get; set; }
        public AwardedPointsDto? PointsAwarded { get; set; }
    }
}