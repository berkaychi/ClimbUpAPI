using AutoMapper;
using ClimbUpAPI.Data;
using ClimbUpAPI.Models;
using ClimbUpAPI.Models.DTOs.SessionDTOs;
using ClimbUpAPI.Models.DTOs.Admin.SessionTypeDTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClimbUpAPI.Services.Implementations
{
    public class SessionTypeService : ISessionTypeService
    {

        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<SessionTypeService> _logger;

        public SessionTypeService(ApplicationDbContext context, IMapper mapper, ILogger<SessionTypeService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<int> CreateAsync(CreateSessionTypeDto dto, string userId)
        {
            _logger.LogDebug("User {UserId} attempting to create session type with DTO: {@CreateSessionTypeDto}", userId, dto);
            bool exists = await _context.SessionTypes
                .AnyAsync(st => st.UserId == userId && !st.IsSystemDefined && st.Name == dto.Name);

            if (exists)
            {
                var ex = new InvalidOperationException("Bu isimde bir oturum tipi zaten mevcut.");
                _logger.LogWarning(ex, "User {UserId} failed to create session type. Name: {SessionTypeName}. Reason: Already exists.", userId, dto.Name);
                throw ex;
            }

            var sessionType = _mapper.Map<SessionType>(dto);
            sessionType.UserId = userId;
            sessionType.IsSystemDefined = false;
            sessionType.IsActive = true;
            _context.SessionTypes.Add(sessionType);
            await _context.SaveChangesAsync();
            _logger.LogInformation("User {UserId} successfully created session type {SessionTypeId}. Name: {SessionTypeName}", userId, sessionType.Id, sessionType.Name);
            return sessionType.Id;
        }

        public async Task<SessionTypeResponseDto?> UpdateAsync(int id, UpdateSessionTypeDto dto, string userId)
        {
            _logger.LogDebug("User {UserId} attempting to update session type {SessionTypeId} with DTO: {@UpdateSessionTypeDto}", userId, id, dto);
            var sessionType = await _context.SessionTypes.FindAsync(id);

            if (sessionType == null)
            {
                var ex = new KeyNotFoundException("Oturum tipi bulunamadı.");
                _logger.LogWarning(ex, "User {UserId} failed to update session type {SessionTypeId}. Reason: Not found. DTO: {@UpdateSessionTypeDto}", userId, id, dto);
                throw ex;
            }

            if (sessionType.UserId != userId)
            {
                var ex = new UnauthorizedAccessException("Bu oturum tipini güncelleme yetkiniz yok.");
                _logger.LogWarning(ex, "User {UserId} unauthorized attempt to update session type {SessionTypeId} owned by {OwnerUserId}. DTO: {@UpdateSessionTypeDto}", userId, id, sessionType.UserId, dto);
                throw ex;
            }

            if (sessionType.IsSystemDefined)
            {
                var ex = new InvalidOperationException("Sistem tanımlı oturum tipleri kullanıcılar tarafından güncellenemez.");
                _logger.LogWarning(ex, "User {UserId} attempt to update system-defined session type {SessionTypeId}. DTO: {@UpdateSessionTypeDto}", userId, id, dto);
                throw ex;
            }

            if (dto.Name != null && dto.Name != sessionType.Name)
            {
                bool nameExists = await _context.SessionTypes
                    .AnyAsync(st => st.UserId == userId && !st.IsSystemDefined && st.Name == dto.Name && st.Id != id);
                if (nameExists)
                {
                    var ex = new InvalidOperationException("Bu isimde başka bir oturum tipiniz zaten mevcut.");
                    _logger.LogWarning(ex, "User {UserId} failed to update session type {SessionTypeId}. Name: {SessionTypeName}. Reason: Name already exists for user.", userId, id, dto.Name);
                    throw ex;
                }
            }

            _mapper.Map(dto, sessionType);
            _context.SessionTypes.Update(sessionType);
            await _context.SaveChangesAsync();
            _logger.LogInformation("User {UserId} successfully updated session type {SessionTypeId}. Name: {SessionTypeName}", userId, id, sessionType.Name);

            return await GetByIdAsync(id, userId);
        }

        public async Task DeleteAsync(int id, string userId)
        {
            _logger.LogDebug("User {UserId} attempting to delete session type {SessionTypeId}.", userId, id);
            var sessionType = await _context.SessionTypes
                .Include(st => st.FocusSessions)
                .FirstOrDefaultAsync(st => st.Id == id);

            if (sessionType == null || !sessionType.IsActive)
            {
                var ex = new KeyNotFoundException("Oturum tipi bulunamadı veya aktif değil.");
                _logger.LogWarning(ex, "User {UserId} failed to delete session type {SessionTypeId}. Reason: Not found or inactive.", userId, id);
                throw ex;
            }

            if (sessionType.IsSystemDefined)
            {
                var isAlreadyHidden = await _context.UserHiddenSystemEntities.AnyAsync(h =>
                    h.UserId == userId && h.EntityId == id && h.EntityType == nameof(SessionType));

                if (isAlreadyHidden)
                {
                    _logger.LogInformation("User {UserId} attempted to hide already hidden system session type {SessionTypeId}. No action taken.", userId, id);
                    return;
                }

                var hiddenEntity = new UserHiddenSystemEntity
                {
                    UserId = userId,
                    EntityId = id,
                    EntityType = nameof(SessionType)
                };
                _context.UserHiddenSystemEntities.Add(hiddenEntity);
                await _context.SaveChangesAsync();
                _logger.LogInformation("User {UserId} successfully hid system session type {SessionTypeId}.", userId, id);
                return;
            }

            if (sessionType.UserId != userId)
            {
                var ex = new UnauthorizedAccessException("Bu oturum tipini silme yetkiniz yok.");
                _logger.LogWarning(ex, "User {UserId} unauthorized attempt to delete session type {SessionTypeId} owned by {OwnerUserId}.", userId, id, sessionType.UserId);
                throw ex;
            }

            if (sessionType.FocusSessions.Any())
            {
                sessionType.IsActive = false;
                _context.SessionTypes.Update(sessionType);
                await _context.SaveChangesAsync();
                _logger.LogInformation("User {UserId} successfully archived session type {SessionTypeId} (formerly delete request). Name: {SessionTypeName} as it is in use by focus sessions.", userId, id, sessionType.Name);
            }
            else
            {
                _context.SessionTypes.Remove(sessionType);
                await _context.SaveChangesAsync();
                _logger.LogInformation("User {UserId} successfully deleted session type {SessionTypeId}. Name: {SessionTypeName} as it was not in use.", userId, id, sessionType.Name);
            }
        }

        public async Task<List<SessionTypeResponseDto>> GetAvailableTypesAsync(string userId)
        {
            _logger.LogDebug("User {UserId} attempting to get available session types.", userId);

            var hiddenSystemTypeIds = await _context.UserHiddenSystemEntities
                .Where(h => h.UserId == userId && h.EntityType == nameof(SessionType))
                .Select(h => h.EntityId)
                .ToListAsync();

            var allActiveTypesQuery = _context.SessionTypes
                .Where(st => st.IsActive && (st.IsSystemDefined || st.UserId == userId))
                .Where(st => !hiddenSystemTypeIds.Contains(st.Id));

            var userScoresQuery = _context.UserSessionTypeUsages
                .Where(ustu => ustu.UserId == userId);

            var typesWithScores = await allActiveTypesQuery
                .GroupJoin(userScoresQuery,
                    sessionType => sessionType.Id,
                    userUsage => userUsage.SessionTypeId,
                    (sessionType, userUsages) => new { SessionType = sessionType, UserUsage = userUsages.FirstOrDefault() })
                .Select(joined => new
                {
                    joined.SessionType,
                    Score = joined.UserUsage != null ? joined.UserUsage.Score : (double?)null
                })
                .ToListAsync();

            var sortedTypes = typesWithScores
                .OrderByDescending(x => x.Score ?? double.MinValue)
                .ThenByDescending(x => x.SessionType.IsSystemDefined)
                .ThenBy(x => x.SessionType.Name)
                .Select(x => x.SessionType)
                .ToList();

            var responseDtos = _mapper.Map<List<SessionTypeResponseDto>>(sortedTypes);
            _logger.LogInformation("User {UserId} successfully retrieved {SessionTypeCount} available session types, sorted by usage score.", userId, responseDtos.Count);
            return responseDtos;
        }

        public async Task<SessionTypeResponseDto?> GetByIdAsync(int id, string userId)
        {
            _logger.LogDebug("User {UserId} attempting to get session type {SessionTypeId}.", userId, id);
            var sessionType = await _context.SessionTypes.FirstOrDefaultAsync(st => st.Id == id && st.IsActive);

            if (sessionType == null)
            {
                _logger.LogWarning("User {UserId} failed to get session type {SessionTypeId}. Reason: Not found or inactive.", userId, id);
                return null;
            }

            if (sessionType.IsSystemDefined)
            {
                var isHidden = await _context.UserHiddenSystemEntities.AnyAsync(h =>
                    h.UserId == userId && h.EntityId == id && h.EntityType == nameof(SessionType));
                if (isHidden)
                {
                    _logger.LogWarning("User {UserId} attempted to access hidden system session type {SessionTypeId}. Access denied.", userId, id);
                    return null;
                }
            }
            else if (sessionType.UserId != userId)
            {
                var ex = new UnauthorizedAccessException("Bu oturum tipini görüntüleme yetkiniz yok.");
                _logger.LogWarning(ex, "User {UserId} unauthorized attempt to get session type {SessionTypeId} owned by {OwnerUserId}.", userId, id, sessionType.UserId);
                throw ex;
            }

            var responseDto = _mapper.Map<SessionTypeResponseDto>(sessionType);
            _logger.LogInformation("User {UserId} successfully retrieved session type {SessionTypeId}. Name: {SessionTypeName}", userId, id, responseDto.Name);
            return responseDto;
        }

        public async Task<SessionType?> GetActiveSessionTypeEntityAsync(int id, string userId)
        {
            _logger.LogDebug("User {UserId} attempting to get session type entity {SessionTypeId}.", userId, id);
            var sessionType = await _context.SessionTypes.FirstOrDefaultAsync(st => st.Id == id && st.IsActive);

            if (sessionType == null)
            {
                _logger.LogWarning("User {UserId} failed to get session type entity {SessionTypeId}. Reason: Not found or inactive.", userId, id);
                return null;
            }

            if (sessionType.IsSystemDefined)
            {
                var isHidden = await _context.UserHiddenSystemEntities.AnyAsync(h =>
                    h.UserId == userId && h.EntityId == id && h.EntityType == nameof(SessionType));
                if (isHidden)
                {
                    _logger.LogWarning("User {UserId} attempted to access hidden system session type entity {SessionTypeId}. Access denied.", userId, id);
                    return null;
                }
            }
            else if (sessionType.UserId != userId)
            {
                _logger.LogWarning("User {UserId} unauthorized attempt to get session type entity {SessionTypeId} owned by {OwnerUserId}.", userId, id, sessionType.UserId);
                return null;
            }

            _logger.LogInformation("User {UserId} successfully retrieved session type entity {SessionTypeId}. Name: {SessionTypeName}", userId, id, sessionType.Name);
            return sessionType;
        }

        // Admin specific methods
        public async Task<AdminSessionTypeResponseDto> CreateAdminSessionTypeAsync(AdminCreateSessionTypeDto dto)
        {
            _logger.LogInformation("Admin attempting to create session type with DTO: {@AdminCreateSessionTypeDto}", dto);

            bool nameExists;
            if (dto.IsSystemDefined)
            {
                nameExists = await _context.SessionTypes.AnyAsync(st => st.IsSystemDefined && st.Name == dto.Name);
            }
            else
            {
                nameExists = await _context.SessionTypes.AnyAsync(st => !st.IsSystemDefined && st.UserId == null && st.Name == dto.Name);
            }

            if (nameExists)
            {
                var ex = new InvalidOperationException($"Bu isimde bir {(dto.IsSystemDefined ? "sistem tanımlı" : "genel")} oturum tipi zaten mevcut.");
                _logger.LogWarning(ex, "Admin failed to create session type. Name: {SessionTypeName}, IsSystem: {IsSystem}. Reason: Already exists.", dto.Name, dto.IsSystemDefined);
                throw ex;
            }

            var sessionType = _mapper.Map<SessionType>(dto);
            sessionType.UserId = dto.IsSystemDefined ? null : sessionType.UserId;

            _context.SessionTypes.Add(sessionType);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Admin successfully created session type {SessionTypeId}. Name: {SessionTypeName}, IsSystem: {IsSystem}", sessionType.Id, sessionType.Name, sessionType.IsSystemDefined);
            return _mapper.Map<AdminSessionTypeResponseDto>(sessionType);
        }

        public async Task<List<AdminSessionTypeResponseDto>> GetAllSessionTypesForAdminAsync(string scope = "all", bool includeArchived = false)
        {
            _logger.LogInformation("Admin attempting to get all session types. Scope: {Scope}, IncludeArchived: {IncludeArchived}", scope, includeArchived);
            var query = _context.SessionTypes.AsQueryable();

            if (scope.ToLowerInvariant() == "system")
            {
                query = query.Where(st => st.IsSystemDefined || st.UserId == null);
            }

            if (!includeArchived)
            {
                query = query.Where(st => st.IsActive);
            }

            var types = await query
                .OrderByDescending(st => st.IsSystemDefined)
                .ThenBy(st => st.UserId == null)
                .ThenBy(st => st.UserId)
                .ThenBy(st => st.Name)
                .ToListAsync();

            _logger.LogInformation("Admin successfully retrieved {SessionTypeCount} session types for Scope: {Scope}, IncludeArchived: {IncludeArchived}.", types.Count, scope, includeArchived);
            return _mapper.Map<List<AdminSessionTypeResponseDto>>(types);
        }

        public async Task<AdminSessionTypeResponseDto?> GetSessionTypeByIdForAdminAsync(int id)
        {
            _logger.LogInformation("Admin attempting to get session type {SessionTypeId}.", id);
            var sessionType = await _context.SessionTypes.FindAsync(id);

            if (sessionType == null)
            {
                _logger.LogWarning("Admin failed to get session type {SessionTypeId}. Reason: Not found.", id);
                return null;
            }
            _logger.LogInformation("Admin successfully retrieved session type {SessionTypeId}. Name: {SessionTypeName}", id, sessionType.Name);
            return _mapper.Map<AdminSessionTypeResponseDto>(sessionType);
        }

        public async Task<AdminSessionTypeResponseDto> UpdateSessionTypeForAdminAsync(int id, AdminUpdateSessionTypeDto dto)
        {
            _logger.LogInformation("Admin attempting to update session type {SessionTypeId} with DTO: {@AdminUpdateSessionTypeDto}", id, dto);
            var sessionType = await _context.SessionTypes.FindAsync(id);

            if (sessionType == null)
            {
                var ex = new KeyNotFoundException("Oturum tipi bulunamadı.");
                _logger.LogWarning(ex, "Admin failed to update session type {SessionTypeId}. Reason: Not found. DTO: {@AdminUpdateSessionTypeDto}", id, dto);
                throw ex;
            }

            if (dto.Name != null && dto.Name != sessionType.Name)
            {
                bool nameExists;
                bool isTargetSystemDefined = dto.IsSystemDefined ?? sessionType.IsSystemDefined;
                string? targetUserId = (dto.IsSystemDefined == true || (dto.IsSystemDefined == null && sessionType.IsSystemDefined)) ? null : sessionType.UserId;

                if (isTargetSystemDefined)
                {
                    nameExists = await _context.SessionTypes.AnyAsync(st => st.Id != id && st.IsSystemDefined && st.Name == dto.Name);
                }
                else if (targetUserId == null)
                {
                    nameExists = await _context.SessionTypes.AnyAsync(st => st.Id != id && !st.IsSystemDefined && st.UserId == null && st.Name == dto.Name);
                }
                else
                {
                    nameExists = await _context.SessionTypes.AnyAsync(st => st.Id != id && !st.IsSystemDefined && st.UserId == targetUserId && st.Name == dto.Name);
                }

                if (nameExists)
                {
                    var ex = new InvalidOperationException($"Bu isimde başka bir {(isTargetSystemDefined ? "sistem tanımlı" : (targetUserId == null ? "genel" : "kullanıcıya ait"))} oturum tipi zaten mevcut.");
                    _logger.LogWarning(ex, "Admin failed to update session type {SessionTypeId}. Name: {SessionTypeName}. Reason: Name already exists.", id, dto.Name);
                    throw ex;
                }
            }

            _mapper.Map(dto, sessionType);

            if (dto.IsSystemDefined == true)
            {
                sessionType.UserId = null;
            }
            else if (dto.IsSystemDefined == false && sessionType.IsSystemDefined && dto.IsSystemDefined.HasValue)
            {
                sessionType.UserId = null;
            }


            _context.SessionTypes.Update(sessionType);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Admin successfully updated session type {SessionTypeId}. Name: {SessionTypeName}", id, sessionType.Name);
            return _mapper.Map<AdminSessionTypeResponseDto>(sessionType);
        }

        public async Task ArchiveSessionTypeAsync(int id)
        {
            _logger.LogInformation("Admin attempting to archive session type {SessionTypeId}.", id);
            var sessionType = await _context.SessionTypes.FindAsync(id);

            if (sessionType == null)
            {
                var ex = new KeyNotFoundException("Oturum tipi bulunamadı.");
                _logger.LogWarning(ex, "Admin failed to archive session type {SessionTypeId}. Reason: Not found.", id);
                throw ex;
            }

            if (!sessionType.IsActive)
            {
                var ex = new InvalidOperationException("Oturum tipi zaten arşivlenmiş.");
                _logger.LogWarning(ex, "Admin failed to archive session type {SessionTypeId}. Reason: Already archived.", id);
                throw ex;
            }

            sessionType.IsActive = false;
            _context.SessionTypes.Update(sessionType);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Admin successfully archived session type {SessionTypeId}. Name: {SessionTypeName}", id, sessionType.Name);
        }

        public async Task UnarchiveSessionTypeAsync(int id)
        {
            _logger.LogInformation("Admin attempting to unarchive session type {SessionTypeId}.", id);
            var sessionType = await _context.SessionTypes.FindAsync(id);

            if (sessionType == null)
            {
                var ex = new KeyNotFoundException("Oturum tipi bulunamadı.");
                _logger.LogWarning(ex, "Admin failed to unarchive session type {SessionTypeId}. Reason: Not found.", id);
                throw ex;
            }

            if (sessionType.IsActive)
            {
                var ex = new InvalidOperationException("Oturum tipi zaten aktif.");
                _logger.LogWarning(ex, "Admin failed to unarchive session type {SessionTypeId}. Reason: Already active.", id);
                throw ex;
            }

            sessionType.IsActive = true;
            _context.SessionTypes.Update(sessionType);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Admin successfully unarchived session type {SessionTypeId}. Name: {SessionTypeName}", id, sessionType.Name);
        }

        public async Task DeleteSessionTypeForAdminAsync(int id)
        {
            _logger.LogInformation("Admin attempting to delete session type {SessionTypeId}.", id);
            var sessionType = await _context.SessionTypes
                .Include(st => st.FocusSessions)
                .FirstOrDefaultAsync(st => st.Id == id);

            if (sessionType == null)
            {
                var ex = new KeyNotFoundException("Oturum tipi bulunamadı.");
                _logger.LogWarning(ex, "Admin failed to delete session type {SessionTypeId}. Reason: Not found.", id);
                throw ex;
            }

            if (sessionType.IsSystemDefined)
            {
                var ex = new InvalidOperationException("Sistem tanımlı oturum tipleri silinemez. Lütfen arşivleyin.");
                _logger.LogWarning(ex, "Admin attempt to delete system-defined session type {SessionTypeId}.", id);
                throw ex;
            }

            if (sessionType.FocusSessions.Any())
            {
                var ex = new InvalidOperationException("Bu oturum tipi, mevcut odak oturumları tarafından kullanıldığı için silinemez. Lütfen önce arşivleyin.");
                _logger.LogWarning(ex, "Admin failed to delete session type {SessionTypeId}. Reason: In use by focus sessions.", id);
                throw ex;
            }

            _context.SessionTypes.Remove(sessionType);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Admin successfully deleted session type {SessionTypeId}. Name: {SessionTypeName}", id, sessionType.Name);
        }
    }
}
