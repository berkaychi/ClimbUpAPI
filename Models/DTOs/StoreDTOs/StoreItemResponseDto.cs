namespace ClimbUpAPI.Models.DTOs.StoreDTOs
{
    public class StoreItemResponseDto
    {
        public int StoreItemId { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public int PriceSS { get; set; }
        public string? IconUrl { get; set; }
        public bool IsConsumable { get; set; }
        public int? MaxQuantityPerUser { get; set; }
        public string? EffectDetails { get; set; }
    }
}