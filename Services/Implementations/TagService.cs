using AutoMapper;
using ClimbUpAPI.Data;
using ClimbUpAPI.Models;
using ClimbUpAPI.Models.DTOs.TagDTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClimbUpAPI.Services.Implementations
{
    public class TagService : ITagService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<TagService> _logger;

        public TagService(ApplicationDbContext context, IMapper mapper, ILogger<TagService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<int> CreateAsync(CreateTagDto dto, string userId)
        {
            _logger.LogDebug("User {UserId} attempting to create tag with DTO: {@CreateTagDto}", userId, dto);

            bool userTagExists = await _context.Tags.AnyAsync(t => t.Name == dto.Name && t.UserId == userId && !t.IsSystemDefined);
            if (userTagExists)
            {
                var ex = new InvalidOperationException("Bu isimde bir etiket zaten mevcut.");
                _logger.LogWarning(ex, "User {UserId} failed to create tag. Name: {TagName}. Reason: User tag with this name already exists. DTO: {@CreateTagDto}", userId, dto.Name, dto);
                throw ex;
            }

            bool systemTagExists = await _context.Tags.AnyAsync(t => t.Name == dto.Name && t.IsSystemDefined && !t.IsArchived);
            if (systemTagExists)
            {
                var ex = new InvalidOperationException($"'{dto.Name}' is a system-defined tag and cannot be used.");
                _logger.LogWarning(ex, "User {UserId} failed to create tag. Name: {TagName}. Reason: Active system tag with this name already exists. DTO: {@CreateTagDto}", userId, dto.Name, dto);
                throw ex;
            }

            var tag = _mapper.Map<Tag>(dto);
            tag.IsSystemDefined = false;
            tag.UserId = userId;

            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();
            _logger.LogInformation("User {UserId} successfully created tag {TagId}. Name: {TagName}, Color: {TagColor}", userId, tag.Id, tag.Name, tag.Color);
            return tag.Id;
        }

        public async Task DeleteAsync(int id, string userId)
        {
            _logger.LogDebug("User {UserId} attempting to delete tag {TagId}.", userId, id);
            var tag = await _context.Tags.FindAsync(id);

            if (tag == null)
            {
                var ex = new KeyNotFoundException("Etiket bulunamadı.");
                _logger.LogWarning(ex, "User {UserId} failed to delete tag {TagId}. Reason: Not found.", userId, id);
                throw ex;
            }

            if (tag.IsSystemDefined)
            {
                var isAlreadyHidden = await _context.UserHiddenSystemEntities.AnyAsync(h =>
                    h.UserId == userId && h.EntityId == id && h.EntityType == nameof(Tag));

                if (isAlreadyHidden)
                {
                    _logger.LogInformation("User {UserId} attempted to hide already hidden system tag {TagId}. No action taken.", userId, id);
                    return;
                }

                var hiddenEntity = new UserHiddenSystemEntity
                {
                    UserId = userId,
                    EntityId = id,
                    EntityType = nameof(Tag)
                };
                _context.UserHiddenSystemEntities.Add(hiddenEntity);
                await _context.SaveChangesAsync();
                _logger.LogInformation("User {UserId} successfully hid system tag {TagId}.", userId, id);
                return;
            }

            if (tag.UserId != userId)
            {
                var ex = new UnauthorizedAccessException("Bu etiketi silme yetkiniz yok.");
                _logger.LogWarning(ex, "User {UserId} unauthorized attempt to delete tag {TagId} owned by {OwnerUserId}.", userId, id, tag.UserId);
                throw ex;
            }

            bool isUsedInFocus = await _context.FocusSessionTags.AnyAsync(fst => fst.TagId == id);
            bool isUsedInToDo = await _context.ToDoTags.AnyAsync(tdt => tdt.TagId == id);

            if (isUsedInFocus || isUsedInToDo)
            {
                tag.IsArchived = true;
                _context.Tags.Update(tag);
                await _context.SaveChangesAsync();
                _logger.LogInformation("User {UserId} successfully archived tag {TagId} (formerly delete request). Name: {TagName} as it is in use.", userId, id, tag.Name);
            }
            else
            {
                _context.Tags.Remove(tag);
                await _context.SaveChangesAsync();
                _logger.LogInformation("User {UserId} successfully deleted tag {TagId}. Name: {TagName} as it was not in use.", userId, id, tag.Name);
            }
        }

        public async Task<List<TagDto>> GetAvailableTagsAsync(string userId)
        {
            _logger.LogDebug("User {UserId} attempting to get available tags.", userId);

            var hiddenSystemTagIds = await _context.UserHiddenSystemEntities
                .Where(h => h.UserId == userId && h.EntityType == nameof(Tag))
                .Select(h => h.EntityId)
                .ToListAsync();

            var allActiveTagsQuery = _context.Tags
                .Where(t => (t.IsSystemDefined && !t.IsArchived) || (t.UserId == userId && !t.IsArchived))
                .Where(t => !hiddenSystemTagIds.Contains(t.Id));

            var userScoresQuery = _context.UserTagUsages
                .Where(utu => utu.UserId == userId);

            var tagsWithScores = await allActiveTagsQuery
                .GroupJoin(userScoresQuery,
                    tag => tag.Id,
                    userUsage => userUsage.TagId,
                    (tag, userUsages) => new { Tag = tag, UserUsage = userUsages.FirstOrDefault() })
                .Select(joined => new
                {
                    joined.Tag,
                    Score = joined.UserUsage != null ? joined.UserUsage.Score : (double?)null
                })
                .ToListAsync();

            var sortedTags = tagsWithScores
                .OrderByDescending(x => x.Score ?? double.MinValue)
                .ThenByDescending(x => x.Tag.IsSystemDefined)
                .ThenBy(x => x.Tag.Name)
                .Select(x => x.Tag)
                .ToList();

            var responseDtos = _mapper.Map<List<TagDto>>(sortedTags);
            _logger.LogInformation("User {UserId} successfully retrieved {TagCount} available tags, sorted by usage score.", userId, responseDtos.Count);
            if (_logger.IsEnabled(LogLevel.Debug) && responseDtos.Any())
            {
                _logger.LogDebug("Available tags for User {UserId}: {@TagList}", userId, responseDtos);
            }
            return responseDtos;
        }

        public async Task<TagDto?> GetByIdAsync(int id, string userId)
        {
            _logger.LogDebug("User {UserId} attempting to get tag {TagId}.", userId, id);
            var tag = await _context.Tags
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tag == null)
            {
                _logger.LogWarning("User {UserId} failed to get tag {TagId}. Reason: Not found.", userId, id);
                return null;
            }

            var isOwner = tag.UserId == userId;
            var isVisibleSystemTag = tag.IsSystemDefined && !tag.IsArchived;

            if (!isOwner && !isVisibleSystemTag)
            {
                _logger.LogWarning("User {UserId} unauthorized attempt to get tag {TagId} owned by {OwnerUserId}.", userId, id, tag.UserId);
                return null;
            }

            if (isVisibleSystemTag)
            {
                var isHidden = await _context.UserHiddenSystemEntities.AnyAsync(h =>
                    h.UserId == userId && h.EntityId == id && h.EntityType == nameof(Tag));
                if (isHidden)
                {
                    _logger.LogWarning("User {UserId} attempted to access hidden system tag {TagId}. Access denied.", userId, id);
                    return null;
                }
            }

            var responseDto = _mapper.Map<TagDto>(tag);
            _logger.LogInformation("User {UserId} successfully retrieved tag {TagId}. Name: {TagName}, Color: {TagColor}", userId, id, responseDto.Name, responseDto.Color);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Full TagDto for {TagId}, User {UserId}: {@TagDto}", id, userId, responseDto);
            }
            return responseDto;
        }

        public async Task UpdateAsync(int id, UpdateTagDto dto, string userId)
        {
            _logger.LogDebug("User {UserId} attempting to update tag {TagId} with DTO: {@UpdateTagDto}", userId, id, dto);
            var tag = await _context.Tags.FindAsync(id);

            if (tag == null)
            {
                var ex = new KeyNotFoundException("Etiket bulunamadı.");
                _logger.LogWarning(ex, "User {UserId} failed to update tag {TagId}. Reason: Not found. DTO: {@UpdateTagDto}", userId, id, dto);
                throw ex;
            }

            if (tag.IsSystemDefined)
            {
                var ex = new InvalidOperationException("Sistem tanımlı etiketler güncellenemez.");
                _logger.LogWarning(ex, "User {UserId} attempt to update system-defined tag {TagId}. DTO: {@UpdateTagDto}", userId, id, dto);
                throw ex;
            }

            if (tag.UserId != userId)
            {
                var ex = new UnauthorizedAccessException("Bu etiketi güncelleme yetkiniz yok.");
                _logger.LogWarning(ex, "User {UserId} unauthorized attempt to update tag {TagId} owned by {OwnerUserId}. DTO: {@UpdateTagDto}", userId, id, tag.UserId, dto);
                throw ex;
            }

            if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != tag.Name)
            {
                bool nameExists = await _context.Tags.AnyAsync(t => t.Name == dto.Name && t.UserId == userId && t.Id != id);
                if (nameExists)
                {
                    var ex = new InvalidOperationException("Bu isimde başka bir etiket zaten mevcut.");
                    _logger.LogWarning(ex, "User {UserId} failed to update tag {TagId}. Reason: Name '{NewTagName}' already exists. DTO: {@UpdateTagDto}", userId, id, dto.Name, dto);
                    throw ex;
                }
            }

            _mapper.Map(dto, tag);
            await _context.SaveChangesAsync();
            _logger.LogInformation("User {UserId} successfully updated tag {TagId}. Name: {TagName}, Color: {TagColor}", userId, id, tag.Name, tag.Color);
        }
        // For Admins: System-defined tags
        public async Task<TagDto> CreateSystemTagAsync(CreateTagDto dto)
        {
            _logger.LogDebug("Admin attempting to create system tag with DTO: {@CreateTagDto}", dto);

            bool exists = await _context.Tags.AnyAsync(t => t.Name == dto.Name && t.IsSystemDefined && !t.IsArchived);
            if (exists)
            {
                var ex = new InvalidOperationException($"A system tag with the name '{dto.Name}' already exists and is active.");
                _logger.LogWarning(ex, "Admin failed to create system tag. Name: {TagName}. Reason: Already exists. DTO: {@CreateTagDto}", dto.Name, dto);
                throw ex;
            }

            var tag = _mapper.Map<Tag>(dto);
            tag.IsSystemDefined = true;
            tag.UserId = null;
            tag.IsArchived = false;

            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin successfully created system tag {TagId}. Name: {TagName}, Color: {TagColor}", tag.Id, tag.Name, tag.Color);
            return _mapper.Map<TagDto>(tag);
        }

        public async Task<TagDto> UpdateSystemTagAsync(int id, UpdateTagDto dto)
        {
            _logger.LogDebug("Admin attempting to update system tag {TagId} with DTO: {@UpdateTagDto}", id, dto);
            var tag = await _context.Tags.FindAsync(id);

            if (tag == null)
            {
                var ex = new KeyNotFoundException("System tag not found.");
                _logger.LogWarning(ex, "Admin failed to update system tag {TagId}. Reason: Not found. DTO: {@UpdateTagDto}", id, dto);
                throw ex;
            }

            if (!tag.IsSystemDefined)
            {
                var ex = new InvalidOperationException("This tag is not a system-defined tag and cannot be updated via this method.");
                _logger.LogWarning(ex, "Admin attempt to update non-system tag {TagId} as system tag. DTO: {@UpdateTagDto}", id, dto);
                throw ex;
            }

            if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != tag.Name)
            {
                bool nameExists = await _context.Tags.AnyAsync(t => t.Name == dto.Name && t.IsSystemDefined && !t.IsArchived && t.Id != id);
                if (nameExists)
                {
                    var ex = new InvalidOperationException($"Another active system tag with the name '{dto.Name}' already exists.");
                    _logger.LogWarning(ex, "Admin failed to update system tag {TagId}. Reason: Name '{NewTagName}' already exists. DTO: {@UpdateTagDto}", id, dto.Name, dto);
                    throw ex;
                }
            }

            _mapper.Map(dto, tag);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin successfully updated system tag {TagId}. Name: {TagName}, Color: {TagColor}", id, tag.Name, tag.Color);
            return _mapper.Map<TagDto>(tag);
        }

        public async Task ArchiveSystemTagAsync(int id)
        {
            _logger.LogDebug("Admin attempting to archive system tag {TagId}", id);
            var tag = await _context.Tags.FindAsync(id);

            if (tag == null)
            {
                var ex = new KeyNotFoundException("System tag not found.");
                _logger.LogWarning(ex, "Admin failed to archive system tag {TagId}. Reason: Not found.", id);
                throw ex;
            }

            if (!tag.IsSystemDefined)
            {
                var ex = new InvalidOperationException("This tag is not a system-defined tag.");
                _logger.LogWarning(ex, "Admin attempt to archive non-system tag {TagId}.", id);
                throw ex;
            }

            if (tag.IsArchived)
            {
                _logger.LogInformation("System tag {TagId} is already archived. No action taken.", id);
                return;
            }


            tag.IsArchived = true;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Admin successfully archived system tag {TagId}.", id);
        }

        public async Task UnarchiveSystemTagAsync(int id)
        {
            _logger.LogDebug("Admin attempting to unarchive system tag {TagId}", id);
            var tag = await _context.Tags.FindAsync(id);

            if (tag == null)
            {
                var ex = new KeyNotFoundException("System tag not found.");
                _logger.LogWarning(ex, "Admin failed to unarchive system tag {TagId}. Reason: Not found.", id);
                throw ex;
            }

            if (!tag.IsSystemDefined)
            {
                var ex = new InvalidOperationException("This tag is not a system-defined tag.");
                _logger.LogWarning(ex, "Admin attempt to unarchive non-system tag {TagId}.", id);
                throw ex;
            }

            if (!tag.IsArchived)
            {
                _logger.LogInformation("System tag {TagId} is not archived. No action taken.", id);
                return;
            }

            bool nameExists = await _context.Tags.AnyAsync(t => t.Name == tag.Name && t.IsSystemDefined && !t.IsArchived && t.Id != id);
            if (nameExists)
            {
                var ex = new InvalidOperationException($"Cannot unarchive system tag '{tag.Name}' (ID: {id}) because another active system tag with the same name already exists. Please rename the conflicting tag first.");
                _logger.LogWarning(ex, "Admin failed to unarchive system tag {TagId} due to name conflict with an active system tag.", id);
                throw ex;
            }

            tag.IsArchived = false;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Admin successfully unarchived system tag {TagId}.", id);
        }

        public async Task DeleteSystemTagAsync(int id)
        {
            _logger.LogDebug("Admin attempting to hard delete system tag {TagId}", id);
            var tag = await _context.Tags.FindAsync(id);

            if (tag == null)
            {
                var ex = new KeyNotFoundException("System tag not found.");
                _logger.LogWarning(ex, "Admin failed to delete system tag {TagId}. Reason: Not found.", id);
                throw ex;
            }

            if (!tag.IsSystemDefined)
            {
                var ex = new InvalidOperationException("This tag is not a system-defined tag.");
                _logger.LogWarning(ex, "Admin attempt to delete non-system tag {TagId} via system delete method.", id);
                throw ex;
            }

            bool isUsedInFocus = await _context.FocusSessionTags.AnyAsync(fst => fst.TagId == id);
            bool isUsedInToDo = await _context.ToDoTags.AnyAsync(tdt => tdt.TagId == id);
            if (isUsedInFocus || isUsedInToDo)
            {
                var ex = new InvalidOperationException("Cannot delete system tag as it is currently in use by Focus Sessions or ToDo Items. Please archive it instead if you wish to make it unavailable.");
                _logger.LogWarning(ex, "Admin failed to delete system tag {TagId}. Reason: In use by FocusSessions: {IsUsedInFocus} or ToDoItems: {IsUsedInToDo}.", id, isUsedInFocus, isUsedInToDo);
                throw ex;
            }

            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Admin successfully hard deleted system tag {TagId}. Name: {TagName}", id, tag.Name);
        }

        public async Task<List<TagDto>> GetSystemTagsAsync(bool includeArchived = false)
        {
            _logger.LogDebug("Admin attempting to get system tags. IncludeArchived: {IncludeArchived}", includeArchived);
            var query = _context.Tags.Where(t => t.IsSystemDefined);

            if (!includeArchived)
            {
                query = query.Where(t => !t.IsArchived);
            }

            var tags = await query.ToListAsync();
            var responseDtos = _mapper.Map<List<TagDto>>(tags);
            _logger.LogInformation("Admin successfully retrieved {TagCount} system tags. IncludeArchived: {IncludeArchived}", responseDtos.Count, includeArchived);
            return responseDtos;
        }

        public async Task<List<TagDto>> GetAllTagsForAdminAsync(string scope = "all", bool includeArchived = false)
        {
            _logger.LogInformation("Admin attempting to get all tags. Scope: {Scope}, IncludeArchived: {IncludeArchived}", scope, includeArchived);
            var query = _context.Tags.AsQueryable();

            if (scope.ToLowerInvariant() == "system")
            {
                query = query.Where(t => t.IsSystemDefined || t.UserId == null);
            }
            if (!includeArchived)
            {
                query = query.Where(t => !t.IsArchived || !t.IsSystemDefined);
            }

            if (!includeArchived)
            {
                query = query.Where(t => (t.IsSystemDefined && !t.IsArchived) || !t.IsSystemDefined);
            }


            var tags = await query
                .OrderByDescending(t => t.IsSystemDefined)
                .ThenBy(t => t.UserId == null)
                .ThenBy(t => t.UserId)
                .ThenBy(t => t.Name)
                .ToListAsync();

            _logger.LogInformation("Admin successfully retrieved {TagCount} total tags for Scope: {Scope}, IncludeArchived: {IncludeArchived}.", tags.Count, scope, includeArchived);
            return _mapper.Map<List<TagDto>>(tags);
        }
    }
}
