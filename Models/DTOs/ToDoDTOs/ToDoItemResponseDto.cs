using ClimbUpAPI.Models.DTOs.TagDTOs;
using System;
using System.Collections.Generic;

namespace ClimbUpAPI.Models.DTOs.ToDoDTOs
{
    public class ToDoItemResponseDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public string Status { get; set; } = null!;

        public bool IsManuallyCompleted { get; set; }

        public DateTime ForDate { get; set; }

        public DateTime? UserIntendedStartTime { get; set; }
        public TimeSpan? TargetWorkDuration { get; set; }
        public TimeSpan AccumulatedWorkDuration { get; set; }


        public List<TagDto>? Tags { get; set; }
    }
}
