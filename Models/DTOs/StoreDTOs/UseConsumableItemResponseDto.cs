namespace ClimbUpAPI.Models.DTOs.StoreDTOs
{
    public class UseConsumableItemResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? EffectActivated { get; set; }
        public int RemainingQuantity { get; set; }
    }
}