using ClimbUpAPI.Models.DTOs.StoreDTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClimbUpAPI.Services
{
    public interface IStoreService
    {
        Task<IEnumerable<StoreItemResponseDto>> GetAvailableStoreItemsAsync();
        Task<PurchaseStoreItemResponseDto> PurchaseStoreItemAsync(string userId, PurchaseStoreItemRequestDto purchaseRequest);
        Task<IEnumerable<UserStoreItemResponseDto>> GetUserPurchasedItemsAsync(string userId);
        Task<UseConsumableItemResponseDto> UseConsumableItemAsync(string userId, UseConsumableItemRequestDto useRequest);
    }
}