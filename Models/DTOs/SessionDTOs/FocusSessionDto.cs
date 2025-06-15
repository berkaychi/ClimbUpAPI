using System;
using System.Collections.Generic;
using ClimbUpAPI.Models.DTOs.TagDTOs;

namespace ClimbUpAPI.Models.DTOs
{
    public class FocusSessionDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public string SessionTypeName { get; set; } = null!;

        public string UserId { get; set; } = null!;

        public string UserName { get; set; } = null!;

        public ICollection<TagDto> Tags { get; set; } = [];
    }
}