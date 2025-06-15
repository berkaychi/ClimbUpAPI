using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ClimbUpAPI.Models;
using System;

namespace ClimbUpAPI.Data.Configurations
{
    public class DefaultSessionTypesConfiguration : IEntityTypeConfiguration<SessionType>
    {
        public void Configure(EntityTypeBuilder<SessionType> builder)
        {
            builder.HasData(
                new SessionType
                {
                    Id = 1,
                    Name = "Classic Pomodoro (25/5)",
                    WorkDuration = (int)TimeSpan.FromMinutes(25).TotalSeconds,
                    BreakDuration = (int)TimeSpan.FromMinutes(5).TotalSeconds,
                    NumberOfCycles = 4,
                    IsSystemDefined = true,
                    IsActive = true,
                    UserId = null
                },
                new SessionType
                {
                    Id = 2,
                    Name = "Deep Work",
                    WorkDuration = (int)TimeSpan.FromMinutes(60).TotalSeconds,
                    BreakDuration = (int)TimeSpan.FromMinutes(10).TotalSeconds,
                    NumberOfCycles = 1,
                    IsSystemDefined = true,
                    IsActive = true,
                    UserId = null
                },
                new SessionType
                {
                    Id = 3,
                    Name = "Quick Focus",
                    WorkDuration = (int)TimeSpan.FromMinutes(15).TotalSeconds,
                    BreakDuration = (int)TimeSpan.FromMinutes(3).TotalSeconds,
                    NumberOfCycles = 1,
                    IsSystemDefined = true,
                    IsActive = true,
                    UserId = null
                },
                new SessionType
                {
                    Id = 4,
                    Name = "Work Sprint",
                    WorkDuration = (int)TimeSpan.FromMinutes(45).TotalSeconds,
                    BreakDuration = (int)TimeSpan.FromMinutes(10).TotalSeconds,
                    NumberOfCycles = 2,
                    IsSystemDefined = true,
                    IsActive = true,
                    UserId = null
                },
                new SessionType
                {
                    Id = 5,
                    Name = "Focus Sprint (25/5)",
                    WorkDuration = (int)TimeSpan.FromMinutes(25).TotalSeconds,
                    BreakDuration = (int)TimeSpan.FromMinutes(5).TotalSeconds,
                    NumberOfCycles = null,
                    IsSystemDefined = true,
                    IsActive = true,
                    UserId = null
                }
            );
        }
    }
}