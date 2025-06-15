using AutoMapper;
using ClimbUpAPI.Models;
using ClimbUpAPI.Models.DTOs;
using ClimbUpAPI.Models.DTOs.SessionDTOs;
using ClimbUpAPI.Models.DTOs.TagDTOs;
using ClimbUpAPI.Models.DTOs.StatisticsDTOs;
using ClimbUpAPI.Models.DTOs.ToDoDTOs;
using ClimbUpAPI.Models.DTOs.Admin.SessionTypeDTOs;
using ClimbUpAPI.Models.DTOs.TaskDTOs;
using ClimbUpAPI.Models.DTOs.UsersDTOs;
using ClimbUpAPI.Models.Badges;
using ClimbUpAPI.Models.DTOs.BadgeDTOs;
using ClimbUpAPI.Models.DTOs.StoreDTOs;
using System.Linq;
namespace ClimbUpAPI.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<CreateSessionTypeDto, SessionType>();
            CreateMap<SessionType, SessionTypeResponseDto>();

            CreateMap<UpdateSessionTypeDto, SessionType>()
                .ForMember(dest => dest.IsActive, opt =>
                {
                    opt.Condition(src => src.IsActive.HasValue);
                    opt.MapFrom(src => src.IsActive!.Value);
                })
                .ForAllMembers(opts =>
                {
                    if (opts.DestinationMember.Name != nameof(SessionType.IsActive))
                    {
                        opts.Condition((src, dest, srcMember) => srcMember != null);
                    }
                });

            // Admin SessionType DTOs
            CreateMap<AdminCreateSessionTypeDto, SessionType>();
            CreateMap<SessionType, AdminSessionTypeResponseDto>();
            CreateMap<AdminUpdateSessionTypeDto, SessionType>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // FocusSession → FocusSessionResponseDto
            CreateMap<FocusSession, FocusSessionResponseDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.SessionTypeName,
                    opt => opt.MapFrom(src => src.SessionType != null ? src.SessionType.Name : null))
                .ForMember(dest => dest.Tags,
                    opt => opt.MapFrom(src => src.FocusSessionTags.Select(fst => fst.Tag)))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.CustomDurationSeconds, opt => opt.MapFrom(src => src.CustomDuration.HasValue ? (int?)src.CustomDuration.Value.TotalSeconds : null))
                .ForMember(dest => dest.CurrentStateEndTime, opt => opt.MapFrom(src => src.CurrentStateEndTime))
                .ForMember(dest => dest.CompletedCycles, opt => opt.MapFrom(src => src.CompletedCycles))
                .ForMember(dest => dest.TotalWorkDuration, opt => opt.MapFrom(src => src.TotalWorkDuration))
                .ForMember(dest => dest.TotalBreakDuration, opt => opt.MapFrom(src => src.TotalBreakDuration));

            // DTO → FocusSession
            CreateMap<CreateFocusSessionDto, FocusSession>()
                .ForMember(dest => dest.ToDoItemId, opt => opt.MapFrom(src => src.ToDoItemId))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.SessionTypeId, opt => opt.MapFrom(src => src.SessionTypeId))
                .ForMember(dest => dest.CustomDuration, opt => opt.MapFrom(src => src.CustomDurationSeconds.HasValue ? (TimeSpan?)TimeSpan.FromSeconds(src.CustomDurationSeconds.Value) : null));

            CreateMap<UpdateFocusSessionDto, FocusSession>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Tag → TagDto
            CreateMap<Tag, TagDto>()
                .ForMember(dest => dest.IsSystemDefined, opt => opt.MapFrom(src => src.IsSystemDefined))
                .ForMember(dest => dest.IsArchived, opt => opt.MapFrom(src => src.IsArchived));
            CreateMap<CreateTagDto, Tag>();
            CreateMap<UpdateTagDto, Tag>()
                 .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));


            // UserStats -> UserStatsSummaryDto
            CreateMap<UserStats, UserStatsSummaryDto>()
                .ForMember(dest => dest.TotalStartedSessions, opt => opt.MapFrom(src => src.TotalStartedSessions))
                .ForMember(dest => dest.SessionCompletionRate, opt => opt.MapFrom(src =>
                    (src.TotalStartedSessions > 0 && src.TotalCompletedSessions > 0) ?
                    Math.Round((double)src.TotalCompletedSessions / src.TotalStartedSessions * 100, 2) : 0))
                .ForMember(dest => dest.AverageSessionDurationSeconds, opt => opt.MapFrom(src =>
                    src.TotalCompletedSessions > 0 ? src.TotalFocusDurationSeconds / src.TotalCompletedSessions : 0))
                .ForMember(dest => dest.LongestSingleSessionDurationSeconds, opt => opt.MapFrom(src => src.LongestSingleSessionDurationSeconds))
                .ForMember(dest => dest.TotalToDosCompletedWithFocus, opt => opt.MapFrom(src => src.TotalToDosCompletedWithFocus));

            // ToDoItem <-> DTOs
            CreateMap<CreateToDoItemDto, ToDoItem>()
                .ForMember(dest => dest.ForDate, opt => opt.MapFrom(src => src.ForDate.Date))
                .ForMember(dest => dest.UserIntendedStartTime, opt => opt.MapFrom(src => src.UserIntendedStartTime))
                .ForMember(dest => dest.TargetWorkDuration, opt => opt.MapFrom(src => src.TargetWorkDuration));

            CreateMap<ToDoItem, ToDoItemResponseDto>()
                .ForMember(dest => dest.ForDate, opt => opt.MapFrom(src => src.ForDate))
                .ForMember(dest => dest.Tags,
                    opt => opt.MapFrom(src => src.ToDoTags.Select(toDoTag => toDoTag.Tag)))
                .ForMember(dest => dest.UserIntendedStartTime, opt => opt.MapFrom(src => src.UserIntendedStartTime))
                .ForMember(dest => dest.TargetWorkDuration, opt => opt.MapFrom(src => src.TargetWorkDuration))
                .ForMember(dest => dest.AccumulatedWorkDuration, opt => opt.MapFrom(src => src.AccumulatedWorkDuration))

                .ForMember(dest => dest.IsManuallyCompleted, opt => opt.MapFrom(src => src.ManuallyCompletedAt.HasValue));

            CreateMap<UpdateToDoItemDto, ToDoItem>()
                .ForMember(dest => dest.ForDate, opt => opt.MapFrom((src, dest) => src.ForDate.HasValue ? src.ForDate.Value.Date : dest.ForDate))
                .ForMember(dest => dest.UserIntendedStartTime, opt => opt.MapFrom(src => src.UserIntendedStartTime))
                .ForMember(dest => dest.TargetWorkDuration, opt => opt.MapFrom(src => src.TargetWorkDuration))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // FocusSession -> FocusSessionSummaryDto
            CreateMap<FocusSession, FocusSessionSummaryDto>()
                .ForMember(dest => dest.ActualStartTime, opt => opt.MapFrom(src => src.StartTime))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.SessionTypeName, opt => opt.MapFrom(src => src.SessionType != null ? src.SessionType.Name : null));

            // AppUser -> UserGetDTO
            CreateMap<AppUser, UserGetDTO>()
                .ForMember(dest => dest.ProfilePictureUrl, opt => opt.MapFrom(src => src.ProfilePictureUrl))
                .ForMember(dest => dest.TotalSteps, opt => opt.MapFrom(src => src.TotalSteps))
                .ForMember(dest => dest.Stepstones, opt => opt.MapFrom(src => src.Stepstones));

            // UpdateProfileDTO -> AppUser
            CreateMap<UpdateProfileDTO, AppUser>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email));

            // Task (Görev) Entity ve DTO'ları için Map'lemeler
            CreateMap<AppTask, AppTaskDto>();
            CreateMap<UserAppTask, UserAppTaskResponseDto>()
                .ForMember(dest => dest.AppTaskDefinition, opt => opt.MapFrom(src => src.AppTaskDefinition));

            // Badge System Mappings
            CreateMap<BadgeLevel, BadgeLevelResponseDto>();

            CreateMap<BadgeDefinition, BadgeDefinitionResponseDto>()
                .ForMember(dest => dest.Levels, opt => opt.MapFrom(src => src.BadgeLevels.OrderBy(l => l.Level)));

            CreateMap<UserBadge, UserBadgeResponseDto>()
                .ForMember(dest => dest.AchievedLevel, opt => opt.MapFrom(src => src.BadgeLevel))
                .ForMember(dest => dest.BadgeDefinitionID, opt => opt.MapFrom(src => src.BadgeLevel.BadgeDefinitionID))
                .ForMember(dest => dest.BadgeCoreName, opt => opt.MapFrom(src =>
                    (src.BadgeLevel != null && src.BadgeLevel.BadgeDefinition != null)
                    ? src.BadgeLevel.BadgeDefinition.CoreName
                    : null));

            // Store System Mappings
            CreateMap<StoreItem, StoreItemResponseDto>();
            CreateMap<UserStoreItem, UserStoreItemResponseDto>()
                .ForMember(dest => dest.StoreItemName, opt => opt.MapFrom(src => src.StoreItem != null ? src.StoreItem.Name : "N/A"))
                .ForMember(dest => dest.StoreItemDescription, opt => opt.MapFrom(src => src.StoreItem != null ? src.StoreItem.Description : null))
                .ForMember(dest => dest.StoreItemIconUrl, opt => opt.MapFrom(src => src.StoreItem != null ? src.StoreItem.IconUrl : null))
                .ForMember(dest => dest.IsConsumable, opt => opt.MapFrom(src => src.StoreItem != null ? src.StoreItem.IsConsumable : false))
                .ForMember(dest => dest.EffectDetails, opt => opt.MapFrom(src => src.StoreItem != null ? src.StoreItem.EffectDetails : null));
        }
    }
}
