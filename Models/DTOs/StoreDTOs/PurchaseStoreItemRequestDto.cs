using System.ComponentModel.DataAnnotations;

namespace ClimbUpAPI.Models.DTOs.StoreDTOs
{
    public class PurchaseStoreItemRequestDto
    {
        [Required]
        public int StoreItemId { get; set; }

        // Opsiyonel: Eğer bir üründen birden fazla alınabiliyorsa
        // public int Quantity { get; set; } = 1;
    }
}