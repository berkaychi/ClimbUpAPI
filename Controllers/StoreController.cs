using ClimbUpAPI.Models.DTOs.StoreDTOs;
using ClimbUpAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ClimbUpAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class StoreController : ControllerBase
    {
        private readonly IStoreService _storeService;
        private readonly ILogger<StoreController> _logger;

        public StoreController(IStoreService storeService, ILogger<StoreController> logger)
        {
            _storeService = storeService;
            _logger = logger;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User ID not found in token.");

        [HttpGet("items")]
        public async Task<ActionResult<IEnumerable<StoreItemResponseDto>>> GetAvailableStoreItems()
        {
            _logger.LogInformation("Endpoint GetAvailableStoreItems called.");
            var items = await _storeService.GetAvailableStoreItemsAsync();
            return Ok(items);
        }

        [HttpGet("my-items")]
        public async Task<ActionResult<IEnumerable<UserStoreItemResponseDto>>> GetUserPurchasedItems()
        {
            var userId = GetUserId();
            _logger.LogInformation("Endpoint GetUserPurchasedItems called by User {UserId}.", userId);
            var items = await _storeService.GetUserPurchasedItemsAsync(userId);
            return Ok(items);
        }

        [HttpPost("purchase")]
        public async Task<ActionResult<PurchaseStoreItemResponseDto>> PurchaseStoreItem([FromBody] PurchaseStoreItemRequestDto purchaseRequest)
        {
            var userId = GetUserId();
            _logger.LogInformation("Endpoint PurchaseStoreItem called by User {UserId} for StoreItem {StoreItemId}.", userId, purchaseRequest.StoreItemId);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _storeService.PurchaseStoreItemAsync(userId, purchaseRequest);

            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("use-consumable")]
        public async Task<ActionResult<UseConsumableItemResponseDto>> UseConsumableItem([FromBody] UseConsumableItemRequestDto useRequest)
        {
            var userId = GetUserId();
            _logger.LogInformation("Endpoint UseConsumableItem called by User {UserId} for StoreItem {StoreItemId}.", userId, useRequest.StoreItemId);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _storeService.UseConsumableItemAsync(userId, useRequest);

            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}