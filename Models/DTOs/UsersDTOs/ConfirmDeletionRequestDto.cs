using System.ComponentModel.DataAnnotations;

namespace ClimbUpAPI.Models.DTOs.UsersDTOs
{
    public class ConfirmDeletionRequestDto
    {
        [Required(ErrorMessage = "Silme token'ı gereklidir.")]
        public string Token { get; set; } = string.Empty;

        public List<string>? DeletionReasons { get; set; }
        public List<string>? MissingFeatures { get; set; }
        public string? FeedbackText { get; set; }
    }
}