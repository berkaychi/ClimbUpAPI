using ClimbUpAPI.Models.DTOs.TagDTOs;
using ClimbUpAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ClimbUpAPI.Controllers.Admin
{
    public class AdminTagController : AdminBaseController
    {
        private readonly ITagService _tagService;
        private readonly ILogger<AdminTagController> _logger;

        public AdminTagController(ITagService tagService, ILogger<AdminTagController> logger)
        {
            _tagService = tagService;
            _logger = logger;
        }

        [HttpPost("system")]
        public async Task<IActionResult> CreateSystemTag([FromBody] CreateTagDto dto)
        {
            _logger.LogInformation("Admin attempting to create system tag: {TagName}", dto.Name);
            try
            {
                var tagDto = await _tagService.CreateSystemTagAsync(dto);
                _logger.LogInformation("Admin successfully created system tag {TagId}: {TagName}", tagDto.Id, tagDto.Name);
                return CreatedAtAction(nameof(GetSystemTagById), new { id = tagDto.Id }, tagDto);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Admin failed to create system tag {TagName} due to invalid operation.", dto.Name);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during system tag creation: {TagName}", dto.Name);
                return StatusCode(500, "An unexpected error occurred while creating the system tag.");
            }
        }

        [HttpGet("system")]
        public async Task<IActionResult> GetSystemTags([FromQuery] bool includeArchived = false)
        {
            _logger.LogInformation("Admin attempting to get system tags. IncludeArchived: {IncludeArchived}", includeArchived);
            try
            {
                var tags = await _tagService.GetSystemTagsAsync(includeArchived);
                return Ok(tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system tags. IncludeArchived: {IncludeArchived}", includeArchived);
                return StatusCode(500, "An unexpected error occurred while retrieving system tags.");
            }
        }

        [HttpGet("system/{id}", Name = "GetSystemTagById")]
        public async Task<IActionResult> GetSystemTagById(int id)
        {
            _logger.LogInformation("Admin attempting to get system tag by ID: {TagId}", id);
            try
            {
                var tags = await _tagService.GetSystemTagsAsync(true);
                var tag = tags.FirstOrDefault(t => t.Id == id);
                if (tag == null)
                {
                    _logger.LogWarning("Admin: System tag with ID {TagId} not found.", id);
                    return NotFound(new { message = "System tag not found." });
                }
                return Ok(tag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system tag by ID: {TagId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }


        [HttpGet("all")]
        public async Task<IActionResult> GetAllTagsForAdmin([FromQuery] string scope = "all", [FromQuery] bool includeArchived = false)
        {
            _logger.LogInformation("Admin attempting to get all tags. Scope: {Scope}, IncludeArchived: {IncludeArchived}", scope, includeArchived);

            string lowerScope = scope.ToLowerInvariant();
            if (lowerScope != "all" && lowerScope != "system")
            {
                _logger.LogWarning("Admin provided invalid scope parameter for GetAllTagsForAdmin: {Scope}.", scope);
                return BadRequest(new { message = "Invalid scope parameter. Allowed values are 'all' or 'system'." });
            }

            try
            {
                var tags = await _tagService.GetAllTagsForAdminAsync(lowerScope, includeArchived);
                _logger.LogInformation("Admin successfully retrieved {TagCount} tags for Scope: {Scope}, IncludeArchived: {IncludeArchived}.", tags.Count, lowerScope, includeArchived);
                return Ok(tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all tags for admin. Scope: {Scope}, IncludeArchived: {IncludeArchived}", lowerScope, includeArchived);
                return StatusCode(500, "An unexpected error occurred while retrieving all tags.");
            }
        }

        [HttpPut("system/{id}")]
        public async Task<IActionResult> UpdateSystemTag(int id, [FromBody] UpdateTagDto dto)
        {
            _logger.LogInformation("Admin attempting to update system tag {TagId}", id);
            try
            {
                var updatedTag = await _tagService.UpdateSystemTagAsync(id, dto);
                _logger.LogInformation("Admin successfully updated system tag {TagId}", id);
                return Ok(updatedTag);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Admin: System tag {TagId} not found for update.", id);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Admin: Invalid operation while updating system tag {TagId}.", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating system tag {TagId}", id);
                return StatusCode(500, "An unexpected error occurred while updating the system tag.");
            }
        }

        [HttpPatch("system/{id}/archive")]
        public async Task<IActionResult> ArchiveSystemTag(int id)
        {
            _logger.LogInformation("Admin attempting to archive system tag {TagId}", id);
            try
            {
                await _tagService.ArchiveSystemTagAsync(id);
                _logger.LogInformation("Admin successfully archived system tag {TagId}", id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Admin: System tag {TagId} not found for archival.", id);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Admin: Invalid operation while archiving system tag {TagId}.", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving system tag {TagId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPatch("system/{id}/unarchive")]
        public async Task<IActionResult> UnarchiveSystemTag(int id)
        {
            _logger.LogInformation("Admin attempting to unarchive system tag {TagId}", id);
            try
            {
                await _tagService.UnarchiveSystemTagAsync(id);
                _logger.LogInformation("Admin successfully unarchived system tag {TagId}", id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Admin: System tag {TagId} not found for unarchival.", id);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Admin: Invalid operation while unarchiving system tag {TagId}.", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unarchiving system tag {TagId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpDelete("system/{id}")]
        public async Task<IActionResult> DeleteSystemTag(int id)
        {
            _logger.LogInformation("Admin attempting to delete system tag {TagId}", id);
            try
            {
                await _tagService.DeleteSystemTagAsync(id);
                _logger.LogInformation("Admin successfully deleted system tag {TagId}", id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Admin: System tag {TagId} not found for deletion.", id);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Admin: Invalid operation while deleting system tag {TagId}.", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting system tag {TagId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}