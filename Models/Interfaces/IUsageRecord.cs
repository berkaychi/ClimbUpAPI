using System;

namespace ClimbUpAPI.Models.Interfaces
{
    public interface IUsageRecord
    {
        bool AwardedFirstUseBonus { get; set; }
        DateTime LastUsedDate { get; set; }
    }
}