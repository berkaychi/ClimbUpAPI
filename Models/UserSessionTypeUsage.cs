using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ClimbUpAPI.Models.Interfaces;

namespace ClimbUpAPI.Models
{
    public class UserSessionTypeUsage : IUsageRecord
    {
        public string UserId { get; set; } = null!;
        public virtual AppUser User { get; set; } = null!;

        public int SessionTypeId { get; set; }
        public virtual SessionType SessionType { get; set; } = null!;

        [Required]
        public double Score { get; set; } = 0.0;

        [Required]
        public DateTime LastUsedDate { get; set; }

        public bool AwardedFirstUseBonus { get; set; } = false;
    }
}