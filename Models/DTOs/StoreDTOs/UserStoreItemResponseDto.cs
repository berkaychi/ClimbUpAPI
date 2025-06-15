using System;

namespace ClimbUpAPI.Models.DTOs.StoreDTOs
{
    public class UserStoreItemResponseDto
    {
        public int StoreItemId { get; set; }
        public required string StoreItemName { get; set; }
        public string? StoreItemDescription { get; set; }
        public string? StoreItemIconUrl { get; set; }
        public DateTime PurchaseDate { get; set; }
        public int Quantity { get; set; }
        public bool IsActive { get; set; }
        public bool IsConsumable { get; set; }
        public string? EffectDetails { get; set; }

    }
}