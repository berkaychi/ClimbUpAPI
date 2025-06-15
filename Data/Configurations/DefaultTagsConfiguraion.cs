using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ClimbUpAPI.Models;

namespace ClimbUpAPI.Data.Configurations
{
    public class DefaultTagsConfiguration : IEntityTypeConfiguration<Tag>
    {
        public void Configure(EntityTypeBuilder<Tag> builder)
        {
            builder.HasData(
                new Tag
                {
                    Id = 1,
                    Name = "Work",
                    Description = "Work-related tasks",
                    IsSystemDefined = true,
                    Color = "#FF5733"
                },
                new Tag
                {
                    Id = 2,
                    Name = "Study",
                    Description = "Study-related tasks",
                    IsSystemDefined = true,
                    Color = "#FF5733"
                },
                new Tag
                {
                    Id = 3,
                    Name = "Personal",
                    Description = "Personal tasks and projects",
                    IsSystemDefined = true,
                    Color = "#FF5733"
                },
                new Tag
                {
                    Id = 4,
                    Name = "Urgent",
                    Description = "High-priority tasks",
                    IsSystemDefined = true,
                    Color = "#FF5733"

                },
                new Tag
                {
                    Id = 5,
                    Name = "Important",
                    Description = "Significant but not necessarily urgent tasks",
                    IsSystemDefined = true,
                    Color = "#FF5733"
                }
            );
        }
    }
}