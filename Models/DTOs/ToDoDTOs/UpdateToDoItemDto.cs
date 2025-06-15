using ClimbUpAPI.Models.Enums;
using System.ComponentModel.DataAnnotations;
using ClimbUpAPI.Helpers.ValidationAttributes;

namespace ClimbUpAPI.Models.DTOs.ToDoDTOs
{
    public class UpdateToDoItemDto
    {
        [StringLength(255, MinimumLength = 1, ErrorMessage = "Başlık en az 1, en fazla 255 karakter olmalıdır.")]
        public string? Title { get; set; }

        public string? Description { get; set; }

        public DateTime? ForDate { get; set; }

        public DateTime? UserIntendedStartTime { get; set; }
        public TimeSpan? TargetWorkDuration { get; set; }
        public List<int>? TagIds { get; set; }

        [NotOverdueStatus]
        public ToDoStatus? Status { get; set; }
    }
}
