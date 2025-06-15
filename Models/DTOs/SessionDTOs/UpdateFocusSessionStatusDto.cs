using System.ComponentModel.DataAnnotations;
using ClimbUpAPI.Models.Enums;

namespace ClimbUpAPI.Models.DTOs.SessionDTOs
{
    public class UpdateFocusSessionStatusDto
    {
        [Required]
        public SessionState Status { get; set; }

        public int? FocusLevel { get; set; }
        public string? ReflectionNotes { get; set; }
    }

}