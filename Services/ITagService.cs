using System.Collections.Generic;
using System.Threading.Tasks;
using ClimbUpAPI.Models.DTOs.TagDTOs;

namespace ClimbUpAPI.Services
{
    public interface ITagService
    {
        Task<int> CreateAsync(CreateTagDto dto, string userId);
        Task<List<TagDto>> GetAvailableTagsAsync(string userId);
        Task<TagDto?> GetByIdAsync(int id, string userId);
        Task UpdateAsync(int id, UpdateTagDto dto, string userId);
        Task DeleteAsync(int id, string userId);

        // Admin specific methods
        Task<TagDto> CreateSystemTagAsync(CreateTagDto dto);
        Task<TagDto> UpdateSystemTagAsync(int id, UpdateTagDto dto);
        Task ArchiveSystemTagAsync(int id);
        Task UnarchiveSystemTagAsync(int id);
        Task DeleteSystemTagAsync(int id);
        Task<List<TagDto>> GetSystemTagsAsync(bool includeArchived = false);
        Task<List<TagDto>> GetAllTagsForAdminAsync(string scope = "all", bool includeArchived = false);
    }
}