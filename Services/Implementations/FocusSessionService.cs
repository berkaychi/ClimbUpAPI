using AutoMapper;
using ClimbUpAPI.Data;
using ClimbUpAPI.Models;
using ClimbUpAPI.Models.DTOs;
using ClimbUpAPI.Models.DTOs.Gamification;
using ClimbUpAPI.Models.DTOs.SessionDTOs;
using ClimbUpAPI.Models.DTOs.TagDTOs;
using ClimbUpAPI.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using MediatR;
using ClimbUpAPI.Application.FocusSessions.Events;

namespace ClimbUpAPI.Services.Implementations
{
    public class FocusSessionService : IFocusSessionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ITagService _tagService;
        private readonly ISessionTypeService _sessionTypeService;
        private readonly IToDoService _toDoService;
        private readonly ILogger<FocusSessionService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IGamificationService _gamificationService;
        private readonly IUserFocusAnalyticsService _userFocusAnalyticsService;
        private readonly IMediator _mediator;
        private const long StreakMinimumDurationSeconds = 25 * 60;

        public FocusSessionService(
            ApplicationDbContext context,
            IMapper mapper,
            ITagService tagService,
            ISessionTypeService sessionTypeService,
            IToDoService toDoService,
            ILogger<FocusSessionService> logger,
            IConfiguration configuration,
            IGamificationService gamificationService,
            IUserFocusAnalyticsService userFocusAnalyticsService,
            IMediator mediator)
        {
            _context = context;
            _mapper = mapper;
            _tagService = tagService;
            _sessionTypeService = sessionTypeService;
            _toDoService = toDoService;
            _logger = logger;
            _configuration = configuration;
            _gamificationService = gamificationService;
            _userFocusAnalyticsService = userFocusAnalyticsService;
            _mediator = mediator;
        }


        public async Task<FocusSessionResponseDto?> GetByIdAsync(int id, string userId)
        {
            _logger.LogDebug("Attempting to get focus session {FocusSessionId} for User {UserId}", id, userId);
            var session = await _context.FocusSessions
                .Include(s => s.SessionType)
                .Include(s => s.FocusSessionTags)
                    .ThenInclude(fst => fst.Tag)
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (session == null)
            {
                _logger.LogWarning("Focus session {FocusSessionId} not found for User {UserId}", id, userId);
                return null;
            }
            var responseDto = _mapper.Map<FocusSessionResponseDto>(session);
            _logger.LogInformation("Successfully retrieved focus session {FocusSessionId} for User {UserId}. StartTime: {StartTime}, EndTime: {EndTime}, Status: {Status}, SessionTypeId: {SessionTypeId}", id, userId, responseDto.StartTime, responseDto.EndTime, responseDto.Status, responseDto.SessionTypeId);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Full FocusSessionResponseDto for {FocusSessionId}, User {UserId}: {@FocusSessionResponseDto}", id, userId, responseDto);
            }
            return responseDto;
        }

        public async Task<List<FocusSessionResponseDto>> GetUserSessionsAsync(string userId)
        {
            _logger.LogDebug("Attempting to get all focus sessions for User {UserId}", userId);
            var sessions = await _context.FocusSessions
            .Where(s => s.UserId == userId)
            .Include(s => s.SessionType)
            .Include(s => s.FocusSessionTags)
                .ThenInclude(fs => fs.Tag)
            .ToListAsync();
            _logger.LogInformation("Successfully retrieved {SessionCount} focus sessions for User {UserId}", sessions.Count, userId);
            if (_logger.IsEnabled(LogLevel.Debug) && sessions.Any())
            {
                _logger.LogDebug("Retrieved focus sessions for User {UserId}: {@FocusSessionList}", userId, _mapper.Map<List<FocusSessionResponseDto>>(sessions));
            }
            return _mapper.Map<List<FocusSessionResponseDto>>(sessions);
        }

        public async Task UpdateAsync(int id, UpdateFocusSessionDto dto, string userId)
        {
            _logger.LogDebug("Attempting to update focus session {FocusSessionId} for User {UserId}. DTO: {@UpdateFocusSessionDto}", id, userId, dto);
            var session = await _context.FocusSessions
                .Include(s => s.FocusSessionTags)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
            {
                var ex = new KeyNotFoundException($"FocusSession with ID {id} not found.");
                _logger.LogWarning(ex, "Focus session {FocusSessionId} not found for update by User {UserId}. DTO: {@UpdateFocusSessionDto}", id, userId, dto);
                throw ex;
            }

            if (session.UserId != userId)
            {
                var ex = new UnauthorizedAccessException("User not authorized to update this session.");
                _logger.LogWarning(ex, "User {UserId} attempted to update unauthorized focus session {FocusSessionId}. DTO: {@UpdateFocusSessionDto}", userId, id, dto);
                throw ex;
            }

            _mapper.Map(dto, session);

            if (dto.ToDoItemId.HasValue)
            {
                var todoItem = await _toDoService.GetByIdAsync(dto.ToDoItemId.Value, userId);
                if (todoItem == null)
                {
                    var ex = new ArgumentException($"ToDoItem with ID {dto.ToDoItemId.Value} not found or does not belong to the user.");
                    _logger.LogWarning(ex, "ToDoItem {ToDoItemId} not found or user {UserId} not authorized during focus session update {FocusSessionId}. DTO: {@UpdateFocusSessionDto}", dto.ToDoItemId.Value, userId, id, dto);
                    throw ex;
                }
                session.ToDoItemId = dto.ToDoItemId.Value;
                _logger.LogDebug("Updating focus session {FocusSessionId}: ToDoItem set to {ToDoItemId} for User {UserId}", id, dto.ToDoItemId.Value, userId);
            }
            else
            {
                if (dto.GetType().GetProperty(nameof(dto.ToDoItemId)) != null && !dto.ToDoItemId.HasValue)
                {
                    session.ToDoItemId = null;
                }
            }

            if (dto.TagIds != null)
            {
                await ValidateTagsAsync(dto.TagIds, userId);
                _logger.LogDebug("Updating tags for focus session {FocusSessionId}, User {UserId}. New TagIds: {@TagIds}", id, userId, dto.TagIds);
                session.FocusSessionTags.Clear();
                foreach (var tagId in dto.TagIds)
                {
                    session.FocusSessionTags.Add(new FocusSessionTag { TagId = tagId, FocusSessionId = session.Id });
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully updated focus session {FocusSessionId} for User {UserId}.", id, userId);
        }

        public async Task<AwardedPointsDto?> UpdateStatusAsync(int id, UpdateFocusSessionStatusDto dto, string userId)
        {
            _logger.LogDebug("Attempting to update status for focus session {FocusSessionId} for User {UserId}. New Status: {NewStatus}, DTO: {@UpdateFocusSessionStatusDto}", id, userId, dto.Status, dto);
            var session = await _context.FocusSessions
               .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
            {
                var ex = new KeyNotFoundException($"FocusSession with ID {id} not found.");
                _logger.LogWarning(ex, "Focus session {FocusSessionId} not found for status update by User {UserId}. DTO: {@UpdateFocusSessionStatusDto}", id, userId, dto);
                throw ex;
            }
            if (session.UserId != userId)
            {
                var ex = new UnauthorizedAccessException("User not authorized to update this session status.");
                _logger.LogWarning(ex, "User {UserId} attempted to update status of unauthorized focus session {FocusSessionId}. DTO: {@UpdateFocusSessionStatusDto}", userId, id, dto);
                throw ex;
            }

            if (session.Status == SessionState.Completed || session.Status == SessionState.Cancelled)
            {
                var ex = new InvalidOperationException($"Bu oturum zaten sonlandırılmış ({session.Status}).");
                _logger.LogWarning(ex, "Attempt to update status of already terminated focus session {FocusSessionId} (Status: {CurrentStatus}) by User {UserId} to {NewStatus}. DTO: {@UpdateFocusSessionStatusDto}", id, session.Status, userId, dto.Status, dto);
                throw ex;
            }

            if (dto.Status == SessionState.Working)
            {
                _logger.LogWarning("Attempt to set session {FocusSessionId} to Working via UpdateStatusAsync. This is unusual. Current Status: {CurrentStatus}", id, session.Status);
            }
            else if (dto.Status == SessionState.Cancelled || dto.Status == SessionState.Completed)
            {
                DateTime now = DateTime.UtcNow;
                long finalWorkPhaseDurationSeconds = 0;

                if (session.Status == SessionState.Working)
                {
                    TimeSpan currentPhaseTimeSpan = now - (session.CurrentPhaseActualStartTime ?? session.StartTime);
                    finalWorkPhaseDurationSeconds = (long)currentPhaseTimeSpan.TotalSeconds;

                    if (finalWorkPhaseDurationSeconds > 0)
                    {
                        session.TotalWorkDuration += (int)finalWorkPhaseDurationSeconds;
                        _logger.LogDebug("Session {SessionId} {Action} while Working. Added {Seconds}s to TotalWorkDuration. New TotalWorkDuration: {TotalWorkDuration}s",
                            session.Id, dto.Status == SessionState.Cancelled ? "cancelled" : "completed", finalWorkPhaseDurationSeconds, session.TotalWorkDuration);

                        await _userFocusAnalyticsService.UpdateStatsForWorkPhaseAsync(userId, finalWorkPhaseDurationSeconds, now);

                        if (session.ToDoItemId.HasValue)
                        {
                            await ProcessToDoUpdateForWorkPhaseAsync(session.ToDoItemId.Value, userId, finalWorkPhaseDurationSeconds, now, session.Id);
                        }
                    }
                }
                else if (session.Status == SessionState.Break && dto.Status == SessionState.Cancelled)
                {
                    TimeSpan currentPhaseTimeSpan = now - (session.CurrentPhaseActualStartTime ?? session.StartTime);
                    session.TotalBreakDuration += (int)currentPhaseTimeSpan.TotalSeconds;
                    _logger.LogDebug("Session {SessionId} cancelled while on Break. Added {Seconds}s to TotalBreakDuration. New TotalBreakDuration: {TotalBreakDuration}s", session.Id, (int)currentPhaseTimeSpan.TotalSeconds, session.TotalBreakDuration);
                }

                session.EndTime = now;
                session.CurrentStateEndTime = now;
                if (dto.Status == SessionState.Completed)
                {
                    session.FocusLevel = dto.FocusLevel;
                    session.ReflectionNotes = dto.ReflectionNotes;
                }
                _logger.LogInformation("Focus session {FocusSessionId} for User {UserId} {Action}. EndTime: {EndTime}", id, userId, dto.Status == SessionState.Cancelled ? "cancelled" : "completed", session.EndTime);
            }
            else
            {
                _logger.LogWarning("UpdateStatusAsync called with an unhandled target status {TargetStatus} for session {SessionId}. Current status: {CurrentStatus}. Only direct 'Completed' or 'Cancelled' are fully processed here.", dto.Status, id, session.Status);
            }

            session.Status = dto.Status;
            AwardedPointsDto? awardedPoints = null;

            if (session.Status == SessionState.Completed)
            {
                awardedPoints = await UpdateUserStatisticsOnSessionCompletion(session);
                await _mediator.Publish(new FocusSessionCompletedNotification { CompletedSession = session });
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully updated status for focus session {FocusSessionId} for User {UserId} to {NewStatus}", id, userId, session.Status);
            return awardedPoints;
        }

        private async Task<AwardedPointsDto?> UpdateUserStatisticsOnSessionCompletion(FocusSession completedSession)
        {
            if (completedSession == null || !completedSession.EndTime.HasValue)
            {
                _logger.LogError("UpdateUserStatisticsOnSessionCompletion called with null session or session without EndTime. SessionId: {SessionId}", completedSession?.Id);
                return null;
            }

            await _userFocusAnalyticsService.UpdateStatisticsOnSessionCompletionAsync(completedSession);

            string userId = completedSession.UserId;
            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (appUser == null)
            {
                _logger.LogError("AppUser not found for UserId {UserId} during UpdateUserStatisticsOnSessionCompletion. Cannot award Steps/Stepstones.", userId);
                return null;
            }

            var userStats = await _userFocusAnalyticsService.GetOrCreateUserStatsAsync(userId);
            var awardedPoints = await _gamificationService.AwardStepsForSessionCompletionAsync(completedSession, appUser, userStats);

            _logger.LogInformation("UpdateUserStatisticsOnSessionCompletion in FocusSessionService finished for User {UserId}, Session {SessionId}. Analytics and Gamification delegated.",
               userId, completedSession.Id);

            return awardedPoints;
        }

        private async Task ProcessToDoUpdateForWorkPhaseAsync(int toDoItemId, string userId, long workPhaseDurationSeconds, DateTime phaseEndTime, int sessionId)
        {
            if (workPhaseDurationSeconds <= 0) return;

            TimeSpan durationWorked = TimeSpan.FromSeconds(workPhaseDurationSeconds);
            await _toDoService.AccumulateWorkDurationAsync(toDoItemId, userId, durationWorked);
            _logger.LogDebug("Delegated ToDo work accumulation for ToDoItemId {ToDoItemId}, User {UserId}, Duration {DurationWorked} to ToDoService from session {SessionId}",
                toDoItemId, userId, durationWorked, sessionId);
        }

        public async Task<FocusSessionResponseDto?> GetOngoingSessionAsync(string userId)
        {
            _logger.LogDebug("Attempting to get ongoing focus session for User {UserId}", userId);

            var ongoingSession = await _context.FocusSessions
                .Include(fs => fs.SessionType)
                .Include(fs => fs.ToDoItem)
                .FirstOrDefaultAsync(fs => fs.UserId == userId && (fs.Status == SessionState.Working || fs.Status == SessionState.Break));

            if (ongoingSession == null)
            {
                _logger.LogInformation("No ongoing focus session found for User {UserId}", userId);
                return null;
            }

            _logger.LogInformation("Successfully retrieved ongoing focus session {FocusSessionId} for User {UserId}", ongoingSession.Id, userId);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Retrieved ongoing focus session for User {UserId}: {@FocusSession}", userId, _mapper.Map<FocusSessionResponseDto>(ongoingSession));
            }
            return _mapper.Map<FocusSessionResponseDto>(ongoingSession);
        }

        public async Task<GamifiedResponse<FocusSessionResponseDto>> CreateFocusSessionAsync(CreateFocusSessionDto dto, string userId)
        {
            _logger.LogDebug("User {UserId} attempting to create a new focus session with DTO: {@CreateFocusSessionDto}", userId, dto);
            var awardedPointsList = new List<AwardedPointsDto>();
            SessionType? sessionTypeDetails = null;

            if (dto.SessionTypeId.HasValue && dto.CustomDurationSeconds.HasValue)
            {
                var ex = new ArgumentException("Either SessionTypeId or CustomDurationSeconds can be provided, but not both.");
                _logger.LogWarning(ex, "Failed to create focus session for User {UserId}. Both SessionTypeId and CustomDurationSeconds were provided. DTO: {@CreateFocusSessionDto}", userId, dto);
                throw ex;
            }

            if (!dto.SessionTypeId.HasValue && !dto.CustomDurationSeconds.HasValue)
            {
                var ex = new ArgumentException("Either SessionTypeId or CustomDurationSeconds must be provided.");
                _logger.LogWarning(ex, "Failed to create focus session for User {UserId}. Neither SessionTypeId nor CustomDurationSeconds were provided. DTO: {@CreateFocusSessionDto}", userId, dto);
                throw ex;
            }

            if (dto.SessionTypeId.HasValue)
            {
                sessionTypeDetails = await _sessionTypeService.GetActiveSessionTypeEntityAsync(dto.SessionTypeId.Value, userId);
                if (sessionTypeDetails == null)
                {
                    var ex = new KeyNotFoundException($"SessionType {dto.SessionTypeId.Value} not found or not accessible by User {userId}.");
                    _logger.LogWarning(ex, "Failed to create focus session due to invalid SessionType {SessionTypeId} for User {UserId}.", dto.SessionTypeId.Value, userId);
                    throw ex;
                }
            }

            if (dto.TagIds != null && dto.TagIds.Any())
            {
                var validatedTags = await ValidateTagsAsync(dto.TagIds, userId);
                foreach (var tag in validatedTags)
                {
                    if (!tag.IsSystemDefined)
                    {
                        var appUserForTagBonus = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                        if (appUserForTagBonus != null)
                        {
                            var tagPoints = await _gamificationService.AwardStepsForNewCustomTagUsageAsync(userId, tag.Id, appUserForTagBonus);
                            if (tagPoints != null) awardedPointsList.Add(tagPoints);
                        }
                        else
                        {
                            _logger.LogWarning("User with ID {UserId} not found when attempting to award steps for new custom tag usage (Tag ID: {TagId}).", userId, tag.Id);
                        }
                    }
                }
            }

            var focusSession = _mapper.Map<FocusSession>(dto);
            focusSession.UserId = userId;
            focusSession.StartTime = DateTime.UtcNow;
            focusSession.Status = SessionState.Working;

            focusSession.TotalWorkDuration = 0;
            focusSession.TotalBreakDuration = 0;
            focusSession.CompletedCycles = 0;
            focusSession.CurrentPhaseActualStartTime = focusSession.StartTime;


            if (sessionTypeDetails != null)
            {
                focusSession.SessionType = sessionTypeDetails;
                focusSession.CurrentStateEndTime = focusSession.StartTime.AddSeconds(sessionTypeDetails.WorkDuration);

                if (!sessionTypeDetails.IsSystemDefined)
                {
                    var appUserForSessionTypeBonus = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                    if (appUserForSessionTypeBonus != null)
                    {
                        var sessionTypePoints = await _gamificationService.AwardStepsForNewCustomSessionTypeUsageAsync(userId, sessionTypeDetails.Id, appUserForSessionTypeBonus);
                        if (sessionTypePoints != null) awardedPointsList.Add(sessionTypePoints);
                    }
                    else
                    {
                        _logger.LogWarning("User with ID {UserId} not found when attempting to award steps for new custom session type usage (SessionType ID: {SessionTypeId}).", userId, sessionTypeDetails.Id);
                    }
                }
            }
            else if (dto.CustomDurationSeconds.HasValue)
            {
                TimeSpan customDuration = TimeSpan.FromSeconds(dto.CustomDurationSeconds.Value);
                focusSession.CustomDuration = customDuration;
                focusSession.CurrentStateEndTime = focusSession.StartTime.Add(customDuration);
            }

            if (focusSession.ToDoItemId.HasValue)
            {
                var toDoItem = await _context.ToDoItems.FirstOrDefaultAsync(t => t.Id == focusSession.ToDoItemId.Value && t.UserId == userId);
                if (toDoItem == null)
                {
                    var ex = new KeyNotFoundException($"ToDoItem {focusSession.ToDoItemId.Value} not found or not owned by User {userId}, though validated earlier.");
                    _logger.LogWarning(ex, "Failed to create focus session for ToDoItem {ToDoItemId} by User {UserId} - item not found post-validation.", focusSession.ToDoItemId.Value, userId);
                    throw ex;
                }
            }

            if (dto.TagIds != null && dto.TagIds.Any())
            {
                focusSession.FocusSessionTags = dto.TagIds.Select(tagId => new FocusSessionTag { TagId = tagId }).ToList();
            }

            _context.FocusSessions.Add(focusSession);

            await _userFocusAnalyticsService.UpdateStatsOnSessionCreationAsync(userId);
            _logger.LogInformation("User {UserId} started a new session. TotalStartedSessions incremented (handled by UserFocusAnalyticsService)", userId);

            await _userFocusAnalyticsService.UpdateUsageScoresAsync(userId, focusSession.SessionTypeId, dto.TagIds);

            await _context.SaveChangesAsync();
            _logger.LogInformation("User {UserId} successfully created FocusSession {FocusSessionId}. Associated ToDoItemId: {ToDoItemId}, Status: {Status}, CurrentStateEndTime: {CurrentStateEndTime}. Usage scores updated via UserFocusAnalyticsService.",
                userId, focusSession.Id, focusSession.ToDoItemId, focusSession.Status, focusSession.CurrentStateEndTime);

            var responseDto = _mapper.Map<FocusSessionResponseDto>(focusSession);
            AwardedPointsDto? finalAwardedPoints = null;
            if (awardedPointsList.Any())
            {
                finalAwardedPoints = new AwardedPointsDto
                {
                    EarnedSteps = awardedPointsList.Sum(p => p.EarnedSteps),
                    EarnedStepstones = awardedPointsList.Sum(p => p.EarnedStepstones),
                    Notification = string.Join(" ", awardedPointsList.Select(p => p.Notification).Where(n => !string.IsNullOrEmpty(n)))
                };
            }

            return new GamifiedResponse<FocusSessionResponseDto>
            {
                Data = responseDto,
                PointsAwarded = finalAwardedPoints
            };
        }

        public async Task<GamifiedResponse<FocusSessionResponseDto>> TransitionSessionStateAsync(int focusSessionId, string userId)
        {
            _logger.LogInformation("Transitioning state for FocusSession {FocusSessionId}, User {UserId}.", focusSessionId, userId);
            var session = await _context.FocusSessions
                .Include(s => s.SessionType)
                .FirstOrDefaultAsync(s => s.Id == focusSessionId && s.UserId == userId);

            if (session == null)
            {
                _logger.LogWarning("FocusSession {FocusSessionId} not found for User {UserId} to transition state.", focusSessionId, userId);
                throw new KeyNotFoundException($"FocusSession with ID {focusSessionId} not found.");
            }

            DateTime now = DateTime.UtcNow;

            if (session.Status == SessionState.Completed || session.Status == SessionState.Cancelled)
            {
                _logger.LogInformation("FocusSession {FocusSessionId} is already {Status}. No state transition performed.", focusSessionId, session.Status);
                return new GamifiedResponse<FocusSessionResponseDto>
                {
                    Data = _mapper.Map<FocusSessionResponseDto>(session),
                    PointsAwarded = null
                };
            }

            bool sessionCompletedInThisTransition = false;
            AwardedPointsDto? awardedPoints = null;

            if (session.CustomDuration.HasValue)
            {
                sessionCompletedInThisTransition = await HandleCustomDurationSessionTransitionAsync(session, userId, now);
            }
            else if (session.SessionTypeId.HasValue && session.SessionType != null)
            {
                sessionCompletedInThisTransition = await HandleSessionTypeBasedTransitionAsync(session, userId, now);
            }
            else
            {
                _logger.LogError("FocusSession {FocusSessionId} has neither SessionType nor CustomDuration. Cannot transition state.", focusSessionId);
                throw new InvalidOperationException("Session has invalid configuration for transitioning state.");
            }

            if (sessionCompletedInThisTransition)
            {
                if (!session.EndTime.HasValue) session.EndTime = now;

                awardedPoints = await UpdateUserStatisticsOnSessionCompletion(session);
                await _mediator.Publish(new FocusSessionCompletedNotification { CompletedSession = session });
                _logger.LogInformation("Published FocusSessionCompletedNotification for User {UserId} after session {FocusSessionId} completion.", session.UserId, session.Id);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("FocusSession {FocusSessionId} processing finished. New Status: {Status}, CurrentStateEndTime: {CurrentStateEndTime}, EndTime: {EndTime}",
                session.Id, session.Status, session.CurrentStateEndTime, session.EndTime);

            var responseDto = _mapper.Map<FocusSessionResponseDto>(session);
            return new GamifiedResponse<FocusSessionResponseDto>
            {
                Data = responseDto,
                PointsAwarded = awardedPoints
            };
        }

        private async Task<bool> HandleSessionTypeBasedTransitionAsync(FocusSession session, string userId, DateTime now)
        {
            var sessionType = session.SessionType;
            if (sessionType == null)
            {
                _logger.LogError("CRITICAL: HandleSessionTypeBasedTransitionAsync called for Session {SessionId} but its SessionType is null, despite having SessionTypeId {SessionTypeId}. Data integrity issue suspected.",
                    session.Id, session.SessionTypeId);
                throw new InvalidOperationException($"Cannot transition state for session {session.Id} because its associated SessionType could not be loaded.");
            }
            bool completedInThisTransition = false;
            long currentPhaseWorkDurationSeconds = 0;

            switch (session.Status)
            {
                case SessionState.Working:
                    TimeSpan completedWorkPhaseTimeSpan = now - (session.CurrentPhaseActualStartTime ?? session.StartTime);
                    currentPhaseWorkDurationSeconds = (long)completedWorkPhaseTimeSpan.TotalSeconds;

                    if (currentPhaseWorkDurationSeconds > 0)
                    {
                        session.TotalWorkDuration += (int)currentPhaseWorkDurationSeconds;
                        _logger.LogDebug("Session {SessionId} transitioning from Working. Added {Seconds}s to TotalWorkDuration. New TotalWorkDuration: {TotalWorkDuration}s", session.Id, (int)currentPhaseWorkDurationSeconds, session.TotalWorkDuration);

                        await _userFocusAnalyticsService.UpdateStatsForWorkPhaseAsync(userId, currentPhaseWorkDurationSeconds, now);
                        if (session.ToDoItemId.HasValue)
                        {
                            await ProcessToDoUpdateForWorkPhaseAsync(session.ToDoItemId.Value, userId, currentPhaseWorkDurationSeconds, now, session.Id);
                        }

                    }

                    session.Status = SessionState.Break;
                    session.CurrentStateEndTime = now.AddSeconds(sessionType!.BreakDuration ?? 0);
                    session.CurrentPhaseActualStartTime = now;
                    _logger.LogInformation("FocusSession {FocusSessionId} transitioned from Working to Break. Next state ends at {NextStateEndTime}", session.Id, session.CurrentStateEndTime);
                    break;

                case SessionState.Break:
                    TimeSpan completedBreakPhaseDuration = now - (session.CurrentPhaseActualStartTime ?? session.StartTime);
                    session.TotalBreakDuration += (int)completedBreakPhaseDuration.TotalSeconds;
                    _logger.LogDebug("Session {SessionId} transitioning from Break. Added {Seconds}s to TotalBreakDuration. New TotalBreakDuration: {TotalBreakDuration}s", session.Id, (int)completedBreakPhaseDuration.TotalSeconds, session.TotalBreakDuration);

                    session.CompletedCycles++;
                    _logger.LogInformation("FocusSession {FocusSessionId} completed a cycle. Total completed cycles: {CompletedCycles}", session.Id, session.CompletedCycles);

                    _logger.LogInformation("Cycle completed for User {UserId}, Session {FocusSessionId}. Total cycles: {CompletedCycles}.", session.UserId, session.Id, session.CompletedCycles);



                    if (sessionType!.NumberOfCycles.HasValue && session.CompletedCycles >= sessionType!.NumberOfCycles.Value)
                    {
                        session.Status = SessionState.Completed;
                        session.EndTime = now;
                        session.CurrentStateEndTime = now;
                        completedInThisTransition = true;
                        _logger.LogInformation("FocusSession {FocusSessionId} reached target cycles ({CompletedCycles}/{NumberOfCycles}) and transitioned to Completed. EndTime: {EndTime}", session.Id, session.CompletedCycles, sessionType.NumberOfCycles.Value, session.EndTime);
                    }
                    else
                    {
                        session.Status = SessionState.Working;
                        session.CurrentStateEndTime = now.AddSeconds(sessionType.WorkDuration);
                        _logger.LogInformation("FocusSession {FocusSessionId} transitioned from Break to Working. Next state ends at {NextStateEndTime}", session.Id, session.CurrentStateEndTime);
                    }
                    session.CurrentPhaseActualStartTime = now;
                    break;

                default:
                    _logger.LogError("FocusSession {FocusSessionId} is in an unexpected state {Status} for SessionType-based transition.", session.Id, session.Status);
                    break;
            }
            return completedInThisTransition;
        }

        private async Task<bool> HandleCustomDurationSessionTransitionAsync(FocusSession session, string userId, DateTime now)
        {
            long workDurationSeconds = (long)session.CustomDuration!.Value.TotalSeconds;
            session.TotalWorkDuration = (int)workDurationSeconds;
            session.TotalBreakDuration = 0;
            session.CompletedCycles = 0;

            if (workDurationSeconds > 0)
            {
                await _userFocusAnalyticsService.UpdateStatsForWorkPhaseAsync(userId, workDurationSeconds, now);
                if (session.ToDoItemId.HasValue)
                {
                    await ProcessToDoUpdateForWorkPhaseAsync(session.ToDoItemId.Value, userId, workDurationSeconds, now, session.Id);
                }
            }

            session.Status = SessionState.Completed;
            session.EndTime = now;
            session.CurrentStateEndTime = now;
            session.CurrentPhaseActualStartTime = session.StartTime;
            _logger.LogInformation("FocusSession {FocusSessionId} (Custom Duration) transitioned to Completed. TotalWorkDuration: {TotalWorkDuration}s. EndTime: {EndTime}", session.Id, session.TotalWorkDuration, session.EndTime);
            return true;
        }
        private async Task<List<TagDto>> ValidateTagsAsync(List<int> tagIds, string userId)
        {
            var validatedTags = new List<TagDto>();
            foreach (var tagId in tagIds)
            {
                var tag = await _tagService.GetByIdAsync(tagId, userId);
                if (tag == null)
                {
                    var ex = new KeyNotFoundException($"Tag {tagId} not found or not accessible by User {userId}.");
                    _logger.LogWarning(ex, "Failed to validate tags due to invalid Tag {TagId} for User {UserId}.", tagId, userId);
                    throw ex;
                }
                validatedTags.Add(tag);
            }
            return validatedTags;
        }
    }
}
