using AutoMapper;
using ClimbUpAPI.Data;
using ClimbUpAPI.Models;
using ClimbUpAPI.Models.DTOs.StoreDTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClimbUpAPI.Services.Implementations
{
    public class StoreService : IStoreService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<StoreService> _logger;

        public StoreService(ApplicationDbContext context, IMapper mapper, ILogger<StoreService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<StoreItemResponseDto>> GetAvailableStoreItemsAsync()
        {
            _logger.LogInformation("Fetching available store items.");
            var storeItems = await _context.StoreItems.AsNoTracking().ToListAsync();
            return _mapper.Map<IEnumerable<StoreItemResponseDto>>(storeItems);
        }

        public async Task<IEnumerable<UserStoreItemResponseDto>> GetUserPurchasedItemsAsync(string userId)
        {
            _logger.LogInformation("Fetching purchased items for User {UserId}.", userId);
            var userItems = await _context.UserStoreItems
                .AsNoTracking()
                .Where(usi => usi.UserId == userId)
                .Include(usi => usi.StoreItem)
                .ToListAsync();

            return _mapper.Map<IEnumerable<UserStoreItemResponseDto>>(userItems);
        }

        public async Task<PurchaseStoreItemResponseDto> PurchaseStoreItemAsync(string userId, PurchaseStoreItemRequestDto purchaseRequest)
        {
            _logger.LogInformation("User {UserId} attempting to purchase StoreItem {StoreItemId}.", userId, purchaseRequest.StoreItemId);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                var storeItem = await _context.StoreItems.FindAsync(purchaseRequest.StoreItemId);

                if (user == null)
                {
                    _logger.LogWarning("Purchase failed: User {UserId} not found.", userId);
                    return new PurchaseStoreItemResponseDto { Success = false, Message = "Kullanıcı bulunamadı." };
                }

                if (storeItem == null)
                {
                    _logger.LogWarning("Purchase failed: StoreItem {StoreItemId} not found.", purchaseRequest.StoreItemId);
                    return new PurchaseStoreItemResponseDto { Success = false, Message = "Mağaza ürünü bulunamadı.", RemainingStepstones = user.Stepstones };
                }

                if (user.Stepstones < storeItem.PriceSS)
                {
                    _logger.LogWarning("Purchase failed: User {UserId} has insufficient Stepstones ({UserStepstones}) to buy StoreItem {StoreItemId} (Price: {ItemPriceSS}).",
                        userId, user.Stepstones, storeItem.StoreItemId, storeItem.PriceSS);
                    return new PurchaseStoreItemResponseDto { Success = false, Message = "Yetersiz Stepstones.", RemainingStepstones = user.Stepstones };
                }

                if (storeItem.MaxQuantityPerUser.HasValue)
                {
                    int currentQuantity = await _context.UserStoreItems
                        .Where(usi => usi.UserId == userId && usi.StoreItemId == storeItem.StoreItemId)
                        .SumAsync(usi => usi.Quantity);

                    if (currentQuantity >= storeItem.MaxQuantityPerUser.Value)
                    {
                        _logger.LogWarning("Purchase failed: User {UserId} reached max quantity for StoreItem {StoreItemId}.", userId, storeItem.StoreItemId);
                        return new PurchaseStoreItemResponseDto { Success = false, Message = "Bu üründen maksimum adete ulaştınız.", RemainingStepstones = user.Stepstones };
                    }
                }

                user.Stepstones -= storeItem.PriceSS;

                UserStoreItem? existingUserStoreItem = null;
                if (storeItem.IsConsumable || storeItem.MaxQuantityPerUser.HasValue)
                {
                    existingUserStoreItem = await _context.UserStoreItems
                       .FirstOrDefaultAsync(usi => usi.UserId == userId && usi.StoreItemId == storeItem.StoreItemId);
                }

                if (existingUserStoreItem != null)
                {
                    existingUserStoreItem.Quantity += 1;
                    existingUserStoreItem.PurchaseDate = DateTime.UtcNow;
                }
                else
                {
                    var newUserStoreItem = new UserStoreItem
                    {
                        UserId = userId,
                        StoreItemId = storeItem.StoreItemId,
                        PurchaseDate = DateTime.UtcNow,
                        Quantity = 1,
                        IsActive = !storeItem.IsConsumable
                    };
                    _context.UserStoreItems.Add(newUserStoreItem);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("User {UserId} successfully purchased StoreItem {StoreItemId}. Remaining Stepstones: {RemainingStepstones}",
                    userId, storeItem.StoreItemId, user.Stepstones);

                return new PurchaseStoreItemResponseDto
                {
                    Success = true,
                    Message = "Satın alma başarılı!",
                    RemainingStepstones = user.Stepstones,
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "An error occurred during the purchase transaction for User {UserId} and StoreItem {StoreItemId}.", userId, purchaseRequest.StoreItemId);
                return new PurchaseStoreItemResponseDto { Success = false, Message = "Satın alma sırasında bir hata oluştu. Lütfen tekrar deneyin." };
            }
        }

        public async Task<UseConsumableItemResponseDto> UseConsumableItemAsync(string userId, UseConsumableItemRequestDto useRequest)
        {
            _logger.LogInformation("User {UserId} attempting to use consumable StoreItem {StoreItemId}.", userId, useRequest.StoreItemId);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    _logger.LogWarning("UseConsumableItem failed: User {UserId} not found.", userId);
                    return new UseConsumableItemResponseDto { Success = false, Message = "Kullanıcı bulunamadı." };
                }

                var userStoreItem = await _context.UserStoreItems
                    .Include(usi => usi.StoreItem)
                    .FirstOrDefaultAsync(usi => usi.UserId == userId && usi.StoreItemId == useRequest.StoreItemId);

                if (userStoreItem == null || userStoreItem.StoreItem == null)
                {
                    _logger.LogWarning("UseConsumableItem failed: User {UserId} does not own StoreItem {StoreItemId}.", userId, useRequest.StoreItemId);
                    return new UseConsumableItemResponseDto { Success = false, Message = "Bu ürüne sahip değilsiniz." };
                }

                if (!userStoreItem.StoreItem.IsConsumable)
                {
                    _logger.LogWarning("UseConsumableItem failed: StoreItem {StoreItemId} is not consumable. User: {UserId}", useRequest.StoreItemId, userId);
                    return new UseConsumableItemResponseDto { Success = false, Message = "Bu ürün tüketilebilir değil." };
                }

                if (userStoreItem.Quantity <= 0)
                {
                    _logger.LogWarning("UseConsumableItem failed: User {UserId} has no quantity left for StoreItem {StoreItemId}.", userId, useRequest.StoreItemId);
                    return new UseConsumableItemResponseDto { Success = false, Message = "Bu üründen kalmamış.", RemainingQuantity = 0 };
                }

                string effectActivatedMessage = "";
                bool effectApplied = false;

                const string CompassItemName = "Pusula";
                const string EnergyBarItemName = "Enerji Barı";

                if (userStoreItem.StoreItem.Name == CompassItemName)
                {
                    if (user.IsCompassActive)
                    {
                        return new UseConsumableItemResponseDto { Success = false, Message = "Pusula zaten aktif.", RemainingQuantity = userStoreItem.Quantity };
                    }
                    user.IsCompassActive = true;
                    effectActivatedMessage = "Pusula etkinleştirildi! Bir sonraki tamamlanan görevin için ekstra Steps kazanacaksın.";
                    effectApplied = true;
                }
                else if (userStoreItem.StoreItem.Name == EnergyBarItemName)
                {
                    if (user.IsEnergyBarActiveForNextSession)
                    {
                        return new UseConsumableItemResponseDto { Success = false, Message = "Enerji Barı zaten bir sonraki seans için aktif.", RemainingQuantity = userStoreItem.Quantity };
                    }
                    user.IsEnergyBarActiveForNextSession = true;
                    effectActivatedMessage = "Enerji Barı bir sonraki odak seansın için hazırlandı! Daha fazla Steps kazanacaksın.";
                    effectApplied = true;
                }
                else
                {
                    _logger.LogWarning("UseConsumableItem: StoreItem {StoreItemId} (Name: {StoreItemName}) has no defined 'use' effect. User: {UserId}",
                        userStoreItem.StoreItemId, userStoreItem.StoreItem.Name, userId);
                    return new UseConsumableItemResponseDto { Success = false, Message = "Bu ürünün bir kullanım etkisi bulunmuyor." };
                }

                if (effectApplied)
                {
                    userStoreItem.Quantity--;
                    if (userStoreItem.Quantity == 0)
                    {
                        _context.UserStoreItems.Remove(userStoreItem);
                        _logger.LogInformation("User {UserId} used the last of StoreItem {StoreItemId} (Name: {StoreItemName}). Item removed from inventory.",
                            userId, userStoreItem.StoreItemId, userStoreItem.StoreItem.Name);
                    }
                    else
                    {
                        _logger.LogInformation("User {UserId} used one StoreItem {StoreItemId} (Name: {StoreItemName}). Remaining quantity: {RemainingQuantity}",
                            userId, userStoreItem.StoreItemId, userStoreItem.StoreItem.Name, userStoreItem.Quantity);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new UseConsumableItemResponseDto
                    {
                        Success = true,
                        Message = "Ürün başarıyla kullanıldı!",
                        EffectActivated = effectActivatedMessage,
                        RemainingQuantity = userStoreItem.Quantity
                    };
                }
                return new UseConsumableItemResponseDto { Success = false, Message = "Ürün kullanılırken bilinmeyen bir hata oluştu." };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "An error occurred during the use consumable transaction for User {UserId} and StoreItem {StoreItemId}.", userId, useRequest.StoreItemId);
                return new UseConsumableItemResponseDto { Success = false, Message = "Ürün kullanılırken bir hata oluştu. Lütfen tekrar deneyin." };
            }
        }
    }
}