using ClimbUpAPI.Models;
using ClimbUpAPI.Models.DTOs.SessionDTOs;
using ClimbUpAPI.Models.DTOs.Admin.SessionTypeDTOs;

namespace ClimbUpAPI.Services
{
    public interface ISessionTypeService
    {
        Task<int> CreateAsync(CreateSessionTypeDto dto, string userId);
        Task<SessionTypeResponseDto?> UpdateAsync(int id, UpdateSessionTypeDto dto, string userId);
        Task DeleteAsync(int id, string userId);
        Task<List<SessionTypeResponseDto>> GetAvailableTypesAsync(string userId);
        Task<SessionTypeResponseDto?> GetByIdAsync(int id, string userId);
        Task<SessionType?> GetActiveSessionTypeEntityAsync(int id, string userId);

        // Admin specific methods
        Task<AdminSessionTypeResponseDto> CreateAdminSessionTypeAsync(AdminCreateSessionTypeDto dto);
        Task<List<AdminSessionTypeResponseDto>> GetAllSessionTypesForAdminAsync(string scope = "all", bool includeArchived = false);
        Task<AdminSessionTypeResponseDto?> GetSessionTypeByIdForAdminAsync(int id);
        Task<AdminSessionTypeResponseDto> UpdateSessionTypeForAdminAsync(int id, AdminUpdateSessionTypeDto dto);
        Task ArchiveSessionTypeAsync(int id);
        Task UnarchiveSessionTypeAsync(int id);
        Task DeleteSessionTypeForAdminAsync(int id);
    }

}