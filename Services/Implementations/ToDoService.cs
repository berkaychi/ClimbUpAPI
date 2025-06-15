using AutoMapper;
using ClimbUpAPI.Data;
using ClimbUpAPI.Models;
using ClimbUpAPI.Models.DTOs.Gamification;
using ClimbUpAPI.Models.DTOs.ToDoDTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClimbUpAPI.Services.Implementations
{
    public class ToDoService : IToDoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ITagService _tagService;
        private readonly ILogger<ToDoService> _logger;
        private readonly IBadgeService _badgeService;
        private readonly IUserFocusAnalyticsService _userFocusAnalyticsService;
        private readonly IGamificationService _gamificationService;

        public ToDoService(
            ApplicationDbContext context,
            IMapper mapper,
            ITagService tagService,
            ILogger<ToDoService> logger,
            IBadgeService badgeService,
            IUserFocusAnalyticsService userFocusAnalyticsService,
            IGamificationService gamificationService)
        {
            _context = context;
            _mapper = mapper;
            _tagService = tagService;
            _logger = logger;
            _badgeService = badgeService;
            _userFocusAnalyticsService = userFocusAnalyticsService;
            _gamificationService = gamificationService;
        }

        public async Task<ToDoItemResponseDto> CreateAsync(CreateToDoItemDto dto, string userId)
        {
            _logger.LogDebug("User {UserId} attempting to create ToDo item with DTO: {@CreateToDoItemDto}", userId, dto);

            if (dto.TagIds != null && dto.TagIds.Any())
            {
                await ValidateTagsAsync(dto.TagIds, userId);
            }

            var toDoItem = _mapper.Map<ToDoItem>(dto);
            toDoItem.UserId = userId;
            toDoItem.Status = Models.Enums.ToDoStatus.Open;
            toDoItem.AccumulatedWorkDuration = TimeSpan.Zero;

            if (dto.ForDate != DateTime.MinValue && dto.ForDate.Year > 1)
            {
                toDoItem.ForDate = dto.ForDate.Date;
                _logger.LogInformation("ForDate for ToDoItem by User {UserId} set to: {ForDate}", userId, toDoItem.ForDate);
            }
            else
            {
                toDoItem.ForDate = DateTime.UtcNow.Date;
                _logger.LogInformation("No valid ForDate provided for new ToDoItem by User {UserId}. Defaulting to UtcNow.Date: {ForDate}", userId, toDoItem.ForDate);
            }

            if (dto.UserIntendedStartTime.HasValue)
            {
                if (dto.UserIntendedStartTime.Value.Date != toDoItem.ForDate)
                {
                    var ex = new ArgumentException("UserIntendedStartTime's date part must be the same as ForDate.");
                    _logger.LogWarning(ex, "Validation failed for UserIntendedStartTime and ForDate for User {UserId} during ToDoItem creation. UserIntendedStartTime: {UserIntendedStartTime}, ForDate: {ForDate}", userId, dto.UserIntendedStartTime, toDoItem.ForDate);
                    throw ex;
                }
                toDoItem.UserIntendedStartTime = dto.UserIntendedStartTime;
            }
            else
            {
                toDoItem.UserIntendedStartTime = null;
            }

            if (dto.TagIds != null && dto.TagIds.Any())
            {
                toDoItem.ToDoTags ??= new List<ToDoTag>();
                foreach (var tagId in dto.TagIds)
                {
                    toDoItem.ToDoTags.Add(new ToDoTag { TagId = tagId });
                }
            }

            _context.ToDoItems.Add(toDoItem);
            await _context.SaveChangesAsync();
            _logger.LogInformation("User {UserId} successfully created ToDoItem {ToDoItemId}: '{ToDoTitle}' with Status {Status}", userId, toDoItem.Id, toDoItem.Title, toDoItem.Status);
            return _mapper.Map<ToDoItemResponseDto>(toDoItem);
        }

        public async Task<ToDoItemResponseDto?> GetByIdAsync(int id, string userId)
        {
            _logger.LogDebug("User {UserId} attempting to get ToDoItem {ToDoItemId}", userId, id);
            var toDoItem = await _context.ToDoItems
                .Include(t => t.ToDoTags)
                    .ThenInclude(tt => tt.Tag)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (toDoItem == null)
            {
                _logger.LogWarning("ToDoItem {ToDoItemId} not found or not owned by User {UserId}", id, userId);
                return null;
            }

            var responseDto = _mapper.Map<ToDoItemResponseDto>(toDoItem);

            _logger.LogInformation("Successfully retrieved ToDoItem {ToDoItemId} for User {UserId}. Title: '{ToDoTitle}', Status: {ToDoStatus}", id, userId, responseDto.Title, responseDto.Status);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Full ToDoItemResponseDto for {ToDoItemId}, User {UserId}: {@ToDoItemResponseDto}", id, userId, responseDto);
            }
            return responseDto;
        }

        public async Task<List<ToDoItemResponseDto>> GetAllAsync(string userId, DateTime? forDate = null)
        {
            _logger.LogDebug("User {UserId} attempting to get ToDo items. ForDate filter: {ForDate}", userId, forDate);
            var query = _context.ToDoItems
                .Include(t => t.ToDoTags)
                    .ThenInclude(tt => tt.Tag)
                .Where(t => t.UserId == userId);

            if (forDate.HasValue)
            {
                query = query.Where(t => t.ForDate == forDate.Value.Date);
            }

            var toDoItems = await query.ToListAsync();
            var responseDtos = _mapper.Map<List<ToDoItemResponseDto>>(toDoItems);

            _logger.LogInformation("Successfully retrieved {Count} ToDo items for User {UserId}", responseDtos.Count, userId);
            if (_logger.IsEnabled(LogLevel.Debug) && responseDtos.Any())
            {
                _logger.LogDebug("Retrieved ToDoItems for User {UserId}: {@ToDoItemsList}", userId, responseDtos);
            }
            return responseDtos;
        }

        public async System.Threading.Tasks.Task UpdateAsync(int id, UpdateToDoItemDto dto, string userId)
        {
            _logger.LogDebug("User {UserId} attempting to update ToDoItem {ToDoItemId} with DTO: {@UpdateToDoItemDto}", userId, id, dto);
            var toDoItem = await _context.ToDoItems
                .Include(t => t.ToDoTags)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (toDoItem == null)
            {
                var ex = new KeyNotFoundException($"Yapılacak görev {id} bulunamadı.");
                _logger.LogWarning(ex, "ToDoItem {ToDoItemId} not found for update by User {UserId}. DTO: {@UpdateToDoItemDto}", id, userId, dto);
                throw ex;
            }

            if (toDoItem.UserId != userId)
            {
                var ex = new UnauthorizedAccessException($"Bu görevi ({id}) güncelleme yetkiniz yok.");
                _logger.LogWarning(ex, "User {UserId} unauthorized attempt to update ToDoItem {ToDoItemId} owned by {OwnerUserId}. DTO: {@UpdateToDoItemDto}", userId, id, toDoItem.UserId, dto);
                throw ex;
            }

            if (dto.ForDate.HasValue)
            {
                toDoItem.ForDate = dto.ForDate.Value.Date;
                _logger.LogDebug("ToDoItem {ToDoItemId} ForDate updated to {ForDate}", id, toDoItem.ForDate);
            }

            var previousStatus = toDoItem.Status;
            _mapper.Map(dto, toDoItem);

            if (toDoItem.Status == Models.Enums.ToDoStatus.Completed && previousStatus != Models.Enums.ToDoStatus.Completed)
            {
                toDoItem.ManuallyCompletedAt = DateTime.UtcNow;
                toDoItem.AutoCompletedAt = null;

                await _userFocusAnalyticsService.UpdateStatsForManualToDoCompletionAsync(userId, toDoItem.Id);
                if (toDoItem.AccumulatedWorkDuration > TimeSpan.Zero)
                {
                    await _userFocusAnalyticsService.UpdateStatsForFocusCompletedToDoAsync(userId, toDoItem.Id);
                }

                await _badgeService.CheckAndAwardBadgesAsync(userId);
            }
            else if (previousStatus == Models.Enums.ToDoStatus.Completed && toDoItem.Status != Models.Enums.ToDoStatus.Completed)
            {
                toDoItem.ManuallyCompletedAt = null;
                toDoItem.AutoCompletedAt = null;
                _logger.LogInformation("ToDoItem {ToDoItemId} reopened by User {UserId}. Completion timestamps cleared.", toDoItem.Id, userId);
            }


            if (dto.UserIntendedStartTime.HasValue)
            {
                if (dto.UserIntendedStartTime.Value.Date != toDoItem.ForDate.Date)
                {
                    var ex = new ArgumentException("UserIntendedStartTime's date part must be the same as ForDate.");
                    _logger.LogWarning(ex, "Validation failed for UserIntendedStartTime and ForDate for User {UserId} during ToDoItem {ToDoItemId} update. UserIntendedStartTime: {UserIntendedStartTime}, ForDate: {ForDate}", userId, id, dto.UserIntendedStartTime, toDoItem.ForDate);
                    throw ex;
                }
                toDoItem.UserIntendedStartTime = dto.UserIntendedStartTime;
                _logger.LogDebug("ToDoItem {ToDoItemId} UserIntendedStartTime updated to {UserIntendedStartTime}", id, toDoItem.UserIntendedStartTime);
            }
            else if (dto.GetType().GetProperty(nameof(dto.UserIntendedStartTime)) != null)
            {
                toDoItem.UserIntendedStartTime = null;
                _logger.LogDebug("ToDoItem {ToDoItemId} UserIntendedStartTime explicitly set to null", id);
            }

            if (dto.TagIds != null)
            {
                await ValidateTagsAsync(dto.TagIds, userId);
                toDoItem.ToDoTags.Clear();
                foreach (var tagId in dto.TagIds)
                {
                    toDoItem.ToDoTags.Add(new ToDoTag { ToDoItemId = toDoItem.Id, TagId = tagId });
                }
            }
            else
            {
                toDoItem.ToDoTags.Clear();
                _logger.LogDebug("ToDoItem {ToDoItemId} tags cleared during update by User {UserId}", id, userId);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("User {UserId} successfully updated ToDoItem {ToDoItemId}: '{ToDoTitle}'", userId, id, toDoItem.Title);
        }

        public async System.Threading.Tasks.Task DeleteAsync(int id, string userId)
        {
            _logger.LogDebug("User {UserId} attempting to delete ToDoItem {ToDoItemId}", userId, id);
            var toDoItem = await _context.ToDoItems.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (toDoItem == null)
            {
                _logger.LogWarning("ToDoItem {ToDoItemId} not found or not owned by User {UserId} for deletion.", id, userId);
                return;
            }
            if (toDoItem.Status == Models.Enums.ToDoStatus.Completed || toDoItem.Status == Models.Enums.ToDoStatus.Overdue)
            {
                var ex = new InvalidOperationException($"Görev '{toDoItem.Title}' (ID: {id}) '{toDoItem.Status}' durumunda olduğu için silinemez.");
                _logger.LogWarning(ex, "Attempt to delete ToDoItem {ToDoItemId} by User {UserId} failed because its status is {ToDoStatus}", id, userId, toDoItem.Status);
                throw ex;
            }

            bool isUsed = await _context.FocusSessions.AnyAsync(fs => fs.ToDoItemId == id);
            if (isUsed)
            {
                var ex = new InvalidOperationException($"Bu görev ({id}), bir odak oturumu ile ilişkili olduğu için silinemez.");
                _logger.LogWarning(ex, "Attempt to delete ToDoItem {ToDoItemId} by User {UserId} failed because it is used in a FocusSession", id, userId);
                throw ex;
            }

            var toDoTitle = toDoItem.Title;
            _context.ToDoItems.Remove(toDoItem);
            await _context.SaveChangesAsync();
            _logger.LogInformation("User {UserId} successfully deleted ToDoItem {ToDoItemId}: '{ToDoTitle}'", userId, id, toDoTitle);
        }

        public async System.Threading.Tasks.Task MarkToDoManuallyCompletedAsync(int toDoId, string userId)
        {
            _logger.LogDebug("User {UserId} attempting to mark ToDoItem {ToDoItemId} as manually completed.", userId, toDoId);
            var toDoItem = await _context.ToDoItems.FirstOrDefaultAsync(t => t.Id == toDoId && t.UserId == userId);
            if (toDoItem == null)
            {
                var ex = new KeyNotFoundException($"Yapılacak görev {toDoId} bulunamadı veya kullanıcıya ait değil.");
                _logger.LogWarning(ex, "ToDoItem {ToDoItemId} not found or not owned by User {UserId} for manual completion.", toDoId, userId);
                throw ex;
            }

            if (toDoItem.Status != Models.Enums.ToDoStatus.Completed)
            {
                toDoItem.Status = Models.Enums.ToDoStatus.Completed;
                toDoItem.ManuallyCompletedAt = DateTime.UtcNow;
                toDoItem.AutoCompletedAt = null;

                await _userFocusAnalyticsService.UpdateStatsForManualToDoCompletionAsync(userId, toDoItem.Id);
                if (toDoItem.AccumulatedWorkDuration > TimeSpan.Zero)
                {
                    await _userFocusAnalyticsService.UpdateStatsForFocusCompletedToDoAsync(userId, toDoItem.Id);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("User {UserId} successfully marked ToDoItem {ToDoItemId} as manually completed. Triggering badge check.", userId, toDoId);
                await _badgeService.CheckAndAwardBadgesAsync(userId);
            }
            else
            {
                if (!toDoItem.ManuallyCompletedAt.HasValue || DateTime.UtcNow > toDoItem.ManuallyCompletedAt.Value)
                {
                    toDoItem.ManuallyCompletedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("ToDoItem {ToDoItemId} was already completed, ManuallyCompletedAt updated for User {UserId}.", toDoId, userId);
                }
                else
                {
                    _logger.LogInformation("ToDoItem {ToDoItemId} was already marked as completed by User {UserId}. No changes made.", toDoId, userId);
                }
            }
        }

        public async Task<List<ToDoDateSummaryDto>> GetMonthOverviewAsync(string userId, int year, int month)
        {
            _logger.LogDebug("User {UserId} attempting to get ToDo month overview for Year: {Year}, Month: {Month}.", userId, year, month);

            if (month < 1 || month > 12)
            {
                throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");
            }
            if (year < 1 || year > 9999)
            {
                throw new ArgumentOutOfRangeException(nameof(year), "Year is out of valid range.");
            }

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            var toDoItemsInMonth = await _context.ToDoItems
                .Where(t => t.UserId == userId && t.ForDate >= startDate && t.ForDate < endDate)
                .ToListAsync();

            var overview = toDoItemsInMonth
                .GroupBy(t => t.ForDate.Date)
                .Select(g => new ToDoDateSummaryDto
                {
                    Date = g.Key,
                    TaskCount = g.Count()
                })
                .OrderBy(s => s.Date)
                .ToList();

            _logger.LogInformation("Successfully retrieved {Count} date summaries for User {UserId}, Year: {Year}, Month: {Month}.", overview.Count, userId, year, month);
            return overview;
        }

        public async Task<AwardedPointsDto?> AccumulateWorkDurationAsync(int toDoItemId, string userId, TimeSpan durationWorked)
        {
            if (durationWorked <= TimeSpan.Zero)
            {
                _logger.LogDebug("AccumulateWorkDurationAsync called for ToDo {ToDoItemId} with zero or negative duration. No action taken.", toDoItemId);
                return null;
            }

            var toDoItem = await _context.ToDoItems.FirstOrDefaultAsync(t => t.Id == toDoItemId && t.UserId == userId);

            if (toDoItem == null)
            {
                _logger.LogWarning("ToDoItem {ToDoItemId} not found for User {UserId} in AccumulateWorkDurationAsync.", toDoItemId, userId);
                return null;
            }

            if (toDoItem.Status == Models.Enums.ToDoStatus.Completed)
            {
                _logger.LogInformation("ToDoItem {ToDoItemId} is already completed. Work duration not accumulated.", toDoItemId);
                return null;
            }

            if (toDoItem.Status != Models.Enums.ToDoStatus.Open && toDoItem.Status != Models.Enums.ToDoStatus.Overdue)
            {
                _logger.LogWarning("ToDoItem {ToDoItemId} is not in Open or Overdue state (current: {ToDoStatus}). Work duration not accumulated.", toDoItemId, toDoItem.Status);
                return null;
            }

            toDoItem.AccumulatedWorkDuration += durationWorked;
            _logger.LogInformation("ToDoItem {ToDoItemId} AccumulatedWorkDuration updated by {Duration} to {NewAccumulatedDuration}",
                toDoItem.Id, durationWorked, toDoItem.AccumulatedWorkDuration);

            if (toDoItem.TargetWorkDuration.HasValue && toDoItem.AccumulatedWorkDuration >= toDoItem.TargetWorkDuration.Value)
            {
                toDoItem.Status = Models.Enums.ToDoStatus.Completed;
                toDoItem.AutoCompletedAt = DateTime.UtcNow;
                toDoItem.ManuallyCompletedAt = null;

                _logger.LogInformation("ToDoItem {ToDoItemId} automatically COMPLETED due to accumulated work. Target: {TargetDuration}, Accumulated: {AccumulatedDuration}",
                    toDoItem.Id, toDoItem.TargetWorkDuration, toDoItem.AccumulatedWorkDuration);

                await _userFocusAnalyticsService.UpdateStatsForFocusCompletedToDoAsync(userId, toDoItem.Id);

                var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                AwardedPointsDto? awardedPoints = null;
                if (appUser != null)
                {
                    awardedPoints = await _gamificationService.AwardStepsForToDoCompletionWithFocusAsync(userId, toDoItem.Id, appUser);
                }
                else
                {
                    _logger.LogWarning("AppUser not found for UserId {UserId} when awarding ToDo completion bonus in ToDoService. ToDoId: {ToDoItemId}", userId, toDoItem.Id);
                }

                await _badgeService.CheckAndAwardBadgesAsync(userId);
                return awardedPoints;
            }
            return null;
        }

        private async System.Threading.Tasks.Task ValidateTagsAsync(List<int> tagIds, string userId)
        {
            foreach (var tagId in tagIds)
            {
                var tag = await _tagService.GetByIdAsync(tagId, userId);
                if (tag == null)
                {
                    throw new KeyNotFoundException($"Tag with ID {tagId} not found or not accessible by User {userId}.");
                }
            }
        }
    }
}
