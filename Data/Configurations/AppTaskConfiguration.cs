using ClimbUpAPI.Models;
using ClimbUpAPI.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClimbUpAPI.Data.Configurations
{
    public class AppTaskConfiguration : IEntityTypeConfiguration<AppTask>
    {
        public void Configure(EntityTypeBuilder<AppTask> builder)
        {
            builder.HasData(
                new AppTask
                {
                    Id = 1,
                    Title = "Günlük Odak Hedefi",
                    Description = "Günlük toplam 50 dakika odaklanma süresine ulaşın.",
                    TaskType = TaskType.DailyFocusDuration,
                    TargetProgress = 50,
                    Recurrence = "Daily",
                    IsActive = true
                },
                new AppTask
                {
                    Id = 2,
                    Title = "Haftalık Odak Hedefi",
                    Description = "Haftalık toplam 240 dakika (4 saat) odaklanma süresine ulaşın.",
                    TaskType = TaskType.WeeklyFocusDuration,
                    TargetProgress = 240,
                    Recurrence = "Weekly",
                    IsActive = true
                }
            );
        }
    }
}