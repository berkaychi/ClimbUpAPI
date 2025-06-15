using System;

namespace ClimbUpAPI.Models.DTOs.UsersDTOs
{
    public class UserProfileWithPointsDto
    {
        public required string Id { get; set; }
        public required string UserName { get; set; }
        public required string Email { get; set; }
        public required string FullName { get; set; }
        public DateTime DateAdded { get; set; }
        public string? ProfilePictureUrl { get; set; }

        // Gamification Points
        public long TotalSteps { get; set; }
        public long Stepstones { get; set; }
    }
}