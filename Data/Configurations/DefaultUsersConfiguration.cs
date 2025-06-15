using ClimbUpAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace ClimbUpAPI.Data.Configurations
{
    public class DefaultUsersConfiguration : IEntityTypeConfiguration<AppUser>
    {
        public static readonly string AdminUserId = "c7b9a6a0-5b0a-4b0e-8f0a-1e2c3d4e5f6a";

        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
        }
    }
}