using ClimbUpAPI.Models.DTOs.Gamification;
using ClimbUpAPI.Models.DTOs.ToDoDTOs;
using System; // For TimeSpan

namespace ClimbUpAPI.Services
{
    public interface IToDoService
    {
        Task<ToDoItemResponseDto> CreateAsync(CreateToDoItemDto dto, string userId);
        Task<ToDoItemResponseDto?> GetByIdAsync(int id, string userId);
        Task<List<ToDoItemResponseDto>> GetAllAsync(string userId, DateTime? forDate = null);
        Task UpdateAsync(int id, UpdateToDoItemDto dto, string userId);
        Task DeleteAsync(int id, string userId);
        System.Threading.Tasks.Task MarkToDoManuallyCompletedAsync(int toDoId, string userId);
        Task<List<ToDoDateSummaryDto>> GetMonthOverviewAsync(string userId, int year, int month);
        Task<AwardedPointsDto?> AccumulateWorkDurationAsync(int toDoItemId, string userId, TimeSpan durationWorked);
    }
}
