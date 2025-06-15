using ClimbUpAPI.Data;
using ClimbUpAPI.Models;
using ClimbUpAPI.Models.DTOs.Gamification;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Collections.Generic;
using ClimbUpAPI.Models.Interfaces;

namespace ClimbUpAPI.Services.Implementations
{
    public class GamificationService : IGamificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GamificationService> _logger;
        private readonly IUserFocusAnalyticsService _userFocusAnalyticsService;
        private static readonly SortedList<int, int> SessionBonusLevels = new()
        {
            { 5, 5 },
            { 15, 10 },
            { 30, 20 },
            { 45, 30 },
            { 60, 40 }
        };

        public GamificationService(ApplicationDbContext context, ILogger<GamificationService> logger, IUserFocusAnalyticsService userFocusAnalyticsService)
        {
            _context = context;
            _logger = logger;
            _userFocusAnalyticsService = userFocusAnalyticsService;
        }

        public async Task<AwardedPointsDto?> AwardStepsForNewCustomSessionTypeUsageAsync(string userId, int sessionTypeId, AppUser user)
        {
            var usageRecord = await _userFocusAnalyticsService.GetOrCreateSessionTypeUsageAsync(userId, sessionTypeId);
            return await AwardFirstUseBonusAsync(
                user,
                usageRecord,
                50,
                $"Yeni bir seans türü kullandığın için {{0}} Adım ve {{1}} Zirve Taşı kazandın!",
                "SessionType",
                sessionTypeId);
        }

        public async Task<AwardedPointsDto?> AwardStepsForNewCustomTagUsageAsync(string userId, int tagId, AppUser user)
        {
            var usageRecord = await _userFocusAnalyticsService.GetOrCreateTagUsageAsync(userId, tagId);
            return await AwardFirstUseBonusAsync(
                user,
                usageRecord,
                50,
                $"Yeni bir etiket kullandığın için {{0}} Adım ve {{1}} Zirve Taşı kazandın!",
                "Tag",
                tagId);
        }

        private int GetCompletedSessionDurationInMinutes(FocusSession session)
        {
            if (session.TotalWorkDuration > 0)
            {
                return (int)(session.TotalWorkDuration / 60.0);
            }
            if (session.EndTime.HasValue && session.StartTime != DateTime.MinValue && session.EndTime.Value > session.StartTime)
            {
                _logger.LogWarning("GamificationService.GetCompletedSessionDurationInMinutes: Session {SessionId} TotalWorkDuration is zero, falling back to EndTime-StartTime for minute calculation.", session.Id);
                return (int)(session.EndTime.Value - session.StartTime).TotalMinutes;
            }
            _logger.LogWarning("GamificationService.GetCompletedSessionDurationInMinutes: Session {SessionId} has no valid duration for minute calculation.", session.Id);
            return 0;
        }

        public Task<AwardedPointsDto?> AwardStepsForSessionCompletionAsync(FocusSession completedSession, AppUser user, UserStats userStats)
        {
            if (user == null)
            {
                _logger.LogError("AppUser is null in AwardStepsForSessionCompletionAsync for session {SessionId}. Cannot award Steps/Stepstones.", completedSession.Id);
                return Task.FromResult<AwardedPointsDto?>(null);
            }
            if (userStats == null)
            {
                _logger.LogError("UserStats is null in AwardStepsForSessionCompletionAsync for user {UserId}, session {SessionId}. Cannot accurately calculate streak bonus.", user.Id, completedSession.Id);
            }


            int durationInMinutes = GetCompletedSessionDurationInMinutes(completedSession);
            if (durationInMinutes <= 0)
            {
                _logger.LogInformation("Session {SessionId} for User {UserId} had zero or negative duration ({DurationMinutes}m). No steps awarded by GamificationService.", completedSession.Id, user.Id, durationInMinutes);
                return Task.FromResult<AwardedPointsDto?>(null);
            }

            long minuteSteps = durationInMinutes * 1;

            long sessionBonusSteps = SessionBonusLevels
                .Where(kvp => durationInMinutes >= kvp.Key)
                .Select(kvp => kvp.Value)
                .LastOrDefault();

            long baseTotalSteps = minuteSteps + sessionBonusSteps;
            long bonusFromEnergyBar = 0;
            string energyBarNotification = "";

            if (user.IsEnergyBarActiveForNextSession)
            {
                bonusFromEnergyBar = (long)(baseTotalSteps * 0.15);
                baseTotalSteps += bonusFromEnergyBar;
                user.IsEnergyBarActiveForNextSession = false;
                energyBarNotification = $" Enerji barı takviyesiyle +{bonusFromEnergyBar} bonus Adım kazandın!";
                _logger.LogInformation("User {UserId} consumed active Energy Bar for session {SessionId}. Awarding +{BonusSteps} bonus Steps to base session Steps in GamificationService.", user.Id, completedSession.Id, bonusFromEnergyBar);
            }

            double streakMultiplier = 1.0;
            int currentStreak = userStats?.CurrentStreakDays ?? 0;
            if (currentStreak >= 3 && currentStreak <= 6) streakMultiplier = 1.2;
            else if (currentStreak >= 7 && currentStreak <= 13) streakMultiplier = 1.5;
            else if (currentStreak >= 14 && currentStreak <= 29) streakMultiplier = 1.8;
            else if (currentStreak >= 30) streakMultiplier = 2.0;

            long earnedSteps = (long)(baseTotalSteps * streakMultiplier);
            long earnedStepstones = earnedSteps / 10;

            user.TotalSteps += earnedSteps;
            user.Stepstones += earnedStepstones;

            _logger.LogInformation("User {UserId} earned {EarnedSteps} Steps and {EarnedStepstones} Stepstones for completing session {SessionId} in GamificationService. Duration: {DurationMinutes}m, BaseSteps (after EnergyBar if any): {BaseTotalSteps}, EnergyBarBonus: {EnergyBarBonus}, Streak: {CurrentStreak}d, Multiplier: {StreakMultiplier}x",
                user.Id, earnedSteps, earnedStepstones, completedSession.Id, durationInMinutes, baseTotalSteps, bonusFromEnergyBar, currentStreak, streakMultiplier);

            string streakNotification = currentStreak >= 3 ? $" {currentStreak} günlük seri bonusuyla ({streakMultiplier}x) daha da fazla kazandın!" : "";
            return Task.FromResult<AwardedPointsDto?>(new AwardedPointsDto
            {
                EarnedSteps = (int)earnedSteps,
                EarnedStepstones = (int)earnedStepstones,
                Notification = $"Seansı tamamlayarak {earnedSteps} Adım ve {earnedStepstones} Zirve Taşı kazandın!{energyBarNotification}{streakNotification}"
            });
        }

        public Task<AwardedPointsDto?> AwardStepsForToDoCompletionWithFocusAsync(string userId, int toDoItemId, AppUser user)
        {
            if (user == null)
            {
                _logger.LogWarning("AppUser is null in AwardStepsForToDoCompletionWithFocusAsync for User {UserId}, ToDoItem {ToDoItemId}. Cannot award steps.", userId, toDoItemId);
                return Task.FromResult<AwardedPointsDto?>(null);
            }

            long baseTodoCompletionSteps = 20;
            long bonusFromCompass = 0;
            string compassNotification = "";

            if (user.IsCompassActive)
            {
                bonusFromCompass = 25;
                user.IsCompassActive = false;
                compassNotification = $" Pusula takviyesiyle +{bonusFromCompass} bonus Adım kazandın!";
                _logger.LogInformation("User {UserId} consumed active Compass for ToDo {ToDoId}. Awarding +{BonusSteps} bonus Steps in GamificationService.", userId, toDoItemId, bonusFromCompass);
            }

            long totalTodoCompletionSteps = baseTodoCompletionSteps + bonusFromCompass;
            long todoCompletionStepstones = totalTodoCompletionSteps / 10;

            user.TotalSteps += totalTodoCompletionSteps;
            user.Stepstones += todoCompletionStepstones;
            _logger.LogInformation("User {UserId} earned {EarnedSteps} Steps (Base: {BaseSteps}, Compass: {CompassBonus}) and {EarnedStepstones} Stepstones for completing ToDo {ToDoId} with focus (GamificationService).",
                userId, totalTodoCompletionSteps, baseTodoCompletionSteps, bonusFromCompass, todoCompletionStepstones, toDoItemId);

            return Task.FromResult<AwardedPointsDto?>(new AwardedPointsDto
            {
                EarnedSteps = (int)totalTodoCompletionSteps,
                EarnedStepstones = (int)todoCompletionStepstones,
                Notification = $"Görevi odaklanarak tamamladığın için {totalTodoCompletionSteps} Adım ve {todoCompletionStepstones} Zirve Taşı kazandın!{compassNotification}"
            });
        }

        private Task<AwardedPointsDto?> AwardFirstUseBonusAsync(
            AppUser user,
            IUsageRecord usageRecord,
            long steps,
            string notificationFormat,
            string entityName,
            int entityId)
        {
            if (user == null)
            {
                _logger.LogWarning("AppUser is null in AwardFirstUseBonusAsync for {EntityName} {EntityId}. Cannot award steps.", entityName, entityId);
                return Task.FromResult<AwardedPointsDto?>(null);
            }

            if (usageRecord.AwardedFirstUseBonus)
            {
                return Task.FromResult<AwardedPointsDto?>(null);
            }

            long earnedStepstones = steps / 10;

            user.TotalSteps += steps;
            user.Stepstones += earnedStepstones;
            usageRecord.AwardedFirstUseBonus = true;
            usageRecord.LastUsedDate = DateTime.UtcNow;

            _logger.LogInformation("User {UserId} earned {EarnedSteps} Steps and {EarnedStepstones} Stepstones for using new custom {EntityName} {EntityId} for the first time (GamificationService).",
                user.Id, steps, earnedStepstones, entityName, entityId);

            return Task.FromResult<AwardedPointsDto?>(new AwardedPointsDto
            {
                EarnedSteps = (int)steps,
                EarnedStepstones = (int)earnedStepstones,
                Notification = string.Format(notificationFormat, steps, earnedStepstones)
            });
        }
    }
}