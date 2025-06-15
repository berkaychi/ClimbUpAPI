using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ClimbUpAPI.Models.Interfaces;

namespace ClimbUpAPI.Models
{
    public class UserTagUsage : IUsageRecord
    {
        public string UserId { get; set; } = null!;
        public virtual AppUser User { get; set; } = null!;

        public int TagId { get; set; }
        public virtual Tag Tag { get; set; } = null!;

        [Required]
        public double Score { get; set; } = 0.0;

        [Required]
        public DateTime LastUsedDate { get; set; }

        public bool AwardedFirstUseBonus { get; set; } = false;
    }
}