using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClimbUpAPI.Models.DTOs;
using ClimbUpAPI.Services;
using System.Security.Claims;
using ClimbUpAPI.Models.DTOs.TagDTOs;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;

namespace ClimbUpAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TagController : ControllerBase
    {
        private readonly ITagService _tagService;
        private readonly ILogger<TagController> _logger;

        public TagController(ITagService tagService, ILogger<TagController> logger)
        {
            _tagService = tagService;
            _logger = logger;
        }

        private string CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException("User not authenticated");

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogDebug("User {UserId} attempting to get all tags.", CurrentUserId);
            try
            {
                var tags = await _tagService.GetAvailableTagsAsync(CurrentUserId);
                _logger.LogInformation("Successfully retrieved {TagCount} tags for User {UserId}.", tags.Count(), CurrentUserId);
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Tags for User {UserId}: {@Tags}", CurrentUserId, tags);
                }
                return Ok(tags);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt by User {UserId} while getting all tags.", CurrentUserId);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} getting all tags.", CurrentUserId);
                return StatusCode(500, new { message = "Etiketler alınırken bir hata oluştu." });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogDebug("User {UserId} attempting to get tag {TagId}.", CurrentUserId, id);
            try
            {
                var tag = await _tagService.GetByIdAsync(id, CurrentUserId);
                if (tag == null)
                {
                    _logger.LogWarning("Tag {TagId} was not found (returned null) for User {UserId}.", id, CurrentUserId);
                    return NotFound(new { message = $"Etiket {id} bulunamadı." });
                }
                _logger.LogInformation("Successfully retrieved tag {TagId} for User {UserId}. Name: {TagName}, Color: {TagColor}", id, CurrentUserId, tag.Name, tag.Color);
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Full Tag data for {TagId} for User {UserId}: {@Tag}", id, CurrentUserId, tag);
                }
                return Ok(tag);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Tag {TagId} not found for User {UserId}.", id, CurrentUserId);
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access by User {UserId} for tag {TagId}.", CurrentUserId, id);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} getting tag {TagId}.", CurrentUserId, id);
                return StatusCode(500, new { message = "Etiket alınırken bir hata oluştu." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTagDto dto)
        {
            _logger.LogDebug("User {UserId} attempting to create tag with DTO: {@CreateTagDto}", CurrentUserId, dto);
            try
            {
                var id = await _tagService.CreateAsync(dto, CurrentUserId);
                _logger.LogInformation("User {UserId} successfully created tag {TagId}. Name: {TagName}, Color: {TagColor}", CurrentUserId, id, dto.Name, dto.Color);
                var createdTag = await _tagService.GetByIdAsync(id, CurrentUserId);
                if (createdTag == null)
                {
                    _logger.LogError("Failed to retrieve newly created tag {TagId} for user {UserId} immediately after creation.", id, CurrentUserId);
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Yeni oluşturulan etiket doğrulaması sırasında bir hata oluştu." });
                }
                return CreatedAtAction(nameof(GetById), new { id = createdTag.Id }, createdTag);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while User {UserId} creating tag. DTO: {@CreateTagDto}", CurrentUserId, dto);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} creating tag. DTO: {@CreateTagDto}", CurrentUserId, dto);
                return StatusCode(500, new { message = "Etiket oluşturulurken bir hata oluştu." });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTagDto dto)
        {
            _logger.LogDebug("User {UserId} attempting to update tag {TagId} with DTO: {@UpdateTagDto}", CurrentUserId, id, dto);
            try
            {
                await _tagService.UpdateAsync(id, dto, CurrentUserId);
                _logger.LogInformation("User {UserId} successfully updated tag {TagId}. Name: {TagName}, Color: {TagColor}", CurrentUserId, id, dto.Name, dto.Color);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Tag {TagId} not found for update by User {UserId}. DTO: {@UpdateTagDto}", id, CurrentUserId, dto);
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized update attempt by User {UserId} for tag {TagId}. DTO: {@UpdateTagDto}", CurrentUserId, id, dto);
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while User {UserId} updating tag {TagId}. DTO: {@UpdateTagDto}", CurrentUserId, id, dto);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} updating tag {TagId}. DTO: {@UpdateTagDto}", CurrentUserId, id, dto);
                return StatusCode(500, new { message = "Etiket güncellenirken bir hata oluştu." });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogDebug("User {UserId} attempting to delete tag {TagId}.", CurrentUserId, id);
            try
            {
                await _tagService.DeleteAsync(id, CurrentUserId);
                _logger.LogInformation("User {UserId} successfully deleted tag {TagId}.", CurrentUserId, id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Tag {TagId} not found for delete by User {UserId}.", id, CurrentUserId);
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized delete attempt by User {UserId} for tag {TagId}.", CurrentUserId, id);
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while User {UserId} deleting tag {TagId}.", CurrentUserId, id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while User {UserId} deleting tag {TagId}.", CurrentUserId, id);
                return StatusCode(500, new { message = "Etiket silinirken bir hata oluştu." });
            }
        }

    }
}
