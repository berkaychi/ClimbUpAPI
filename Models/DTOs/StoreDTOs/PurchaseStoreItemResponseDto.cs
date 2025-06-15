namespace ClimbUpAPI.Models.DTOs.StoreDTOs
{
    public class PurchaseStoreItemResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public long RemainingStepstones { get; set; }
        public UserStoreItemResponseDto? PurchasedItem { get; set; }
    }
}