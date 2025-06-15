using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ClimbUpAPI.Models;

namespace ClimbUpAPI.Data.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<AppRole>
    {
        public void Configure(EntityTypeBuilder<AppRole> builder)
        {
            builder.HasData(
                new AppRole
                {
                    Id = "871a497c-21a6-4e0a-a9c7-1e6a6b8f1a7e", // Standard Admin Role ID
                    Name = "Admin",
                    NormalizedName = "ADMIN"
                },
                new AppRole
                {
                    Id = "a1b2c3d4-e5f6-7890-1234-567890abcdef", // Standard User Role ID
                    Name = "User",
                    NormalizedName = "USER"
                }
            );
        }
    }
}