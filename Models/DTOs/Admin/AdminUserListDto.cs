using System;
using System.Collections.Generic;

namespace ClimbUpAPI.Models.DTOs.Admin
{
    public class AdminUserListDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;
        public List<string> Roles { get; set; } = [];
        public DateTime DateAdded { get; set; }
    }
}