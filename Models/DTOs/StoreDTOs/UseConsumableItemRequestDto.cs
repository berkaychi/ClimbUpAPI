using System.ComponentModel.DataAnnotations;

namespace ClimbUpAPI.Models.DTOs.StoreDTOs
{
    public class UseConsumableItemRequestDto
    {
        [Required]
        public int StoreItemId { get; set; }
    }
}