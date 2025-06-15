using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ClimbUpAPI.Models;
using ClimbUpAPI.Models.Badges;
using ClimbUpAPI.Data.Configurations;

namespace ClimbUpAPI.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser, AppRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<FocusSession> FocusSessions => Set<FocusSession>();
        public DbSet<SessionType> SessionTypes => Set<SessionType>();
        public DbSet<FocusSessionTag> FocusSessionTags => Set<FocusSessionTag>();
        public DbSet<ToDoItem> ToDoItems => Set<ToDoItem>();
        public DbSet<ToDoTag> ToDoTags => Set<ToDoTag>();
        public DbSet<UserStats> UserStats => Set<UserStats>();
        public DbSet<UserRefreshToken> UserRefreshTokens => Set<UserRefreshToken>();
        public DbSet<AppTask> AppTasks { get; set; }
        public DbSet<UserAppTask> UserAppTasks { get; set; }

        // Badge System DbSets
        public DbSet<BadgeDefinition> BadgeDefinitions { get; set; } = null!;
        public DbSet<BadgeLevel> BadgeLevels { get; set; } = null!;
        public DbSet<UserBadge> UserBadges { get; set; } = null!;
        public DbSet<AccountDeletionRequest> AccountDeletionRequests { get; set; } = null!;

        // Usage Tracking DbSets
        public DbSet<UserSessionTypeUsage> UserSessionTypeUsages { get; set; } = null!;
        public DbSet<UserTagUsage> UserTagUsages { get; set; } = null!;

        // Gamification and Store System DbSets
        public DbSet<StoreItem> StoreItems { get; set; } = null!;
        public DbSet<UserStoreItem> UserStoreItems { get; set; } = null!;
        public DbSet<UserHiddenSystemEntity> UserHiddenSystemEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // FocusSession ↔ Tag (many-to-many)
            builder.Entity<FocusSessionTag>()
                .HasKey(x => new { x.FocusSessionId, x.TagId });

            // FocusSessionTag → FocusSession (many-to-one)
            builder.Entity<FocusSessionTag>()
                .HasOne(fst => fst.FocusSession)
                .WithMany(fs => fs.FocusSessionTags)
                .HasForeignKey(fst => fst.FocusSessionId);

            // FocusSessionTag → Tag (many-to-one)
            builder.Entity<FocusSessionTag>()
                .HasOne(fst => fst.Tag)
                .WithMany(t => t.FocusSessionTags)
                .HasForeignKey(fst => fst.TagId);

            // Tag → AppUser (many-to-one)
            builder.Entity<Tag>()
                .HasOne(t => t.User)
                .WithMany(u => u.Tags)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Tag>()
                .HasIndex(t => new { t.UserId, t.Name })
                .IsUnique()
                .HasFilter("IsSystemDefined = 0");

            // SessionType → AppUser (many-to-one)
            builder.Entity<SessionType>()
                .HasOne(st => st.User)
                .WithMany(u => u.SessionTypes)
                .HasForeignKey(st => st.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            builder.Entity<SessionType>()
                .HasIndex(st => new { st.UserId, st.Name })
                .IsUnique()
                .HasFilter("IsSystemDefined = 0");

            // FocusSession → SessionType (nullable, one-to-many)
            builder.Entity<FocusSession>()
                .HasOne(fs => fs.SessionType)
                .WithMany(st => st.FocusSessions)
                .HasForeignKey(fs => fs.SessionTypeId)
                .OnDelete(DeleteBehavior.SetNull);

            // FocusSession → ToDoItem (nullable, one-to-many)
            builder.Entity<FocusSession>()
                .HasOne(fs => fs.ToDoItem)
                .WithMany(ti => ti.FocusSessions)
                .HasForeignKey(fs => fs.ToDoItemId)
                .OnDelete(DeleteBehavior.SetNull);

            // FocusSession.Status → enum as string
            builder.Entity<FocusSession>()
                .Property(fs => fs.Status)
                .HasConversion<string>();

            builder.Entity<ToDoTag>()
                .HasKey(tt => new { tt.ToDoItemId, tt.TagId });

            // ToDoTag → ToDoItem (many-to-one)
            builder.Entity<ToDoTag>()
                .HasOne(tt => tt.ToDoItem)
                .WithMany(t => t.ToDoTags)
                .HasForeignKey(tt => tt.ToDoItemId);

            // ToDoTag → Tag (many-to-one)
            builder.Entity<ToDoTag>()
                .HasOne(tt => tt.Tag)
                .WithMany()
                .HasForeignKey(tt => tt.TagId);

            // ToDoItem.Status → enum as string
            builder.Entity<ToDoItem>()
                .Property(t => t.Status)
                .HasConversion<string>();

            // ToDoItem → AppUser (many-to-one)
            builder.Entity<ToDoItem>()
                .HasOne(ti => ti.User)
                .WithMany()
                .HasForeignKey(ti => ti.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // FocusSession → AppUser (many-to-one)
            builder.Entity<FocusSession>()
                .HasOne(fs => fs.User)
                .WithMany()
                .HasForeignKey(fs => fs.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
            // UserAppTask ile AppUser arasındaki ilişki
            builder.Entity<UserAppTask>()
                .HasOne(uat => uat.AppUser)
                .WithMany(u => u.UserAppTasks)
                .HasForeignKey(uat => uat.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserAppTask ile AppTask arasındaki ilişki
            builder.Entity<UserAppTask>()
                .HasOne(uat => uat.AppTaskDefinition)
                .WithMany(at => at.UserAppTasks)
                .HasForeignKey(uat => uat.AppTaskId)
                .OnDelete(DeleteBehavior.Cascade);


            builder.ApplyConfiguration(new DefaultSessionTypesConfiguration());
            builder.ApplyConfiguration(new DefaultTagsConfiguration());
            builder.ApplyConfiguration(new RoleConfiguration());
            builder.ApplyConfiguration(new DefaultUsersConfiguration());
            builder.ApplyConfiguration(new AppTaskConfiguration());

            builder.Entity<AppUser>()
                .HasOne(u => u.UserStats)
                .WithOne(us => us.User)
                .HasForeignKey<UserStats>(us => us.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserStats>().HasIndex(us => us.TotalFocusDurationSeconds);
            builder.Entity<UserStats>().HasIndex(us => us.TotalCompletedSessions);
            builder.Entity<UserStats>().HasIndex(us => us.TotalToDosCompleted);

            // Badge System Configurations
            // BadgeDefinition to BadgeLevel (one-to-many)
            builder.Entity<BadgeDefinition>()
                .HasMany(bd => bd.BadgeLevels)
                .WithOne(bl => bl.BadgeDefinition)
                .HasForeignKey(bl => bl.BadgeDefinitionID)
                .OnDelete(DeleteBehavior.Cascade);

            // UserBadge to AppUser (many-to-one)
            builder.Entity<UserBadge>()
                .HasOne(ub => ub.User)
                .WithMany()
                .HasForeignKey(ub => ub.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserBadge to BadgeLevel (many-to-one)
            builder.Entity<UserBadge>()
                .HasOne(ub => ub.BadgeLevel)
                .WithMany(bl => bl.UserBadges)
                .HasForeignKey(ub => ub.BadgeLevelID)
                .OnDelete(DeleteBehavior.Restrict);

            SeedBadgeData(builder);

            // AccountDeletionRequest Configuration
            builder.Entity<AccountDeletionRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // UserSessionTypeUsage Configuration
            builder.Entity<UserSessionTypeUsage>()
                .HasKey(ustu => new { ustu.UserId, ustu.SessionTypeId });

            builder.Entity<UserSessionTypeUsage>()
                .HasOne(ustu => ustu.User)
                .WithMany()
                .HasForeignKey(ustu => ustu.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserSessionTypeUsage>()
                .HasOne(ustu => ustu.SessionType)
                .WithMany()
                .HasForeignKey(ustu => ustu.SessionTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserTagUsage Configuration
            builder.Entity<UserTagUsage>()
                .HasKey(utu => new { utu.UserId, utu.TagId });

            builder.Entity<UserTagUsage>()
                .HasOne(utu => utu.User)
                .WithMany()
                .HasForeignKey(utu => utu.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserTagUsage>()
                .HasOne(utu => utu.Tag)
                .WithMany()
                .HasForeignKey(utu => utu.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserHiddenSystemEntity Configuration
            builder.Entity<UserHiddenSystemEntity>()
                .HasKey(uhse => new { uhse.UserId, uhse.EntityType, uhse.EntityId });

            builder.Entity<UserHiddenSystemEntity>()
                .HasOne(uhse => uhse.User)
                .WithMany()
                .HasForeignKey(uhse => uhse.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Gamification and Store System Configurations
            // AppUser to UserStoreItem (one-to-many)
            builder.Entity<AppUser>()
                .HasMany(u => u.UserStoreItems)
                .WithOne(usi => usi.User)
                .HasForeignKey(usi => usi.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // StoreItem to UserStoreItem (one-to-many)
            builder.Entity<StoreItem>()
                .HasMany(si => si.UserStoreItems)
                .WithOne(usi => usi.StoreItem)
                .HasForeignKey(usi => usi.StoreItemId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserStoreItem (Composite Key)
            builder.Entity<UserStoreItem>()
                .HasKey(usi => new { usi.UserId, usi.StoreItemId });

            builder.Entity<AppUser>().HasIndex(u => u.TotalSteps);
            builder.Entity<AppUser>().HasIndex(u => u.Stepstones);

            SeedStoreItemData(builder);

        }

        private void SeedBadgeData(ModelBuilder builder)
        {
            // Badge Definitions
            var badgeDefFocusSessions = new BadgeDefinition { BadgeDefinitionID = 1, CoreName = "Zirve Akıncısı", MetricToTrack = "completed_focus_sessions", Description = "Tamamlanan odak seansı sayısına göre kazanılır." };
            var badgeDefFocusDuration = new BadgeDefinition { BadgeDefinitionID = 2, CoreName = "İrtifa Koleksiyoncusu", MetricToTrack = "total_focus_duration_hours", Description = "Toplam odaklanma süresine göre kazanılır." };
            var badgeDefToDosCompleted = new BadgeDefinition { BadgeDefinitionID = 3, CoreName = "Malzeme Depocusu", MetricToTrack = "total_todos_completed", Description = "Tamamlanan görev sayısına göre kazanılır." };

            builder.Entity<BadgeDefinition>().HasData(
                badgeDefFocusSessions,
                badgeDefFocusDuration,
                badgeDefToDosCompleted
            );

            // Badge Levels
            // Zirve Akıncısı Levels
            builder.Entity<BadgeLevel>().HasData(
                new BadgeLevel { BadgeLevelID = 1, BadgeDefinitionID = badgeDefFocusSessions.BadgeDefinitionID, /* BadgeDefinition = badgeDefFocusSessions, */ Level = 1, Name = "İlk Adım", Description = "5 odak seansı tamamla.", IconURL = "https://res.cloudinary.com/dp7utrng4/image/upload/v1748396782/ilk_adim_mifzbj.png", RequiredValue = 5 },
                new BadgeLevel { BadgeLevelID = 2, BadgeDefinitionID = badgeDefFocusSessions.BadgeDefinitionID, /* BadgeDefinition = badgeDefFocusSessions, */ Level = 2, Name = "Patika Takipçisi", Description = "20 odak seansı tamamla.", IconURL = "https://res.cloudinary.com/dp7utrng4/image/upload/v1748396777/patika_takipcisi_vn63sd.png", RequiredValue = 20 },
                new BadgeLevel { BadgeLevelID = 3, BadgeDefinitionID = badgeDefFocusSessions.BadgeDefinitionID, /* BadgeDefinition = badgeDefFocusSessions, */ Level = 3, Name = "Kaya Tırmanışçısı", Description = "50 odak seansı tamamla.", IconURL = "https://res.cloudinary.com/dp7utrng4/image/upload/v1748396778/kaya_tirmaniscisi_qmzbty.png", RequiredValue = 50 }
            );

            // İrtifa Koleksiyoncusu Levels
            builder.Entity<BadgeLevel>().HasData(
                new BadgeLevel { BadgeLevelID = 4, BadgeDefinitionID = badgeDefFocusDuration.BadgeDefinitionID, /* BadgeDefinition = badgeDefFocusDuration, */ Level = 1, Name = "Alçak Rakım Gözcüsü", Description = "Toplam 10 saat odaklan.", IconURL = "https://res.cloudinary.com/dp7utrng4/image/upload/v1748396778/alcak_rakim_gozcusu_at9jj1.png", RequiredValue = 10 },
                new BadgeLevel { BadgeLevelID = 5, BadgeDefinitionID = badgeDefFocusDuration.BadgeDefinitionID, /* BadgeDefinition = badgeDefFocusDuration, */ Level = 2, Name = "Orta Rakım Kaşifi", Description = "Toplam 50 saat odaklan.", IconURL = "https://res.cloudinary.com/dp7utrng4/image/upload/v1748396777/orta_rakim_kasifi_chunvy.png", RequiredValue = 50 },
                new BadgeLevel { BadgeLevelID = 6, BadgeDefinitionID = badgeDefFocusDuration.BadgeDefinitionID, /* BadgeDefinition = badgeDefFocusDuration, */ Level = 3, Name = "Yüksek Rakım Uzmanı", Description = "Toplam 150 saat odaklan.", IconURL = "https://res.cloudinary.com/dp7utrng4/image/upload/v1748396778/yuksek_rakim_uzmani_dlkob3.png", RequiredValue = 150 }
            );

            // Malzeme Depocusu Levels
            builder.Entity<BadgeLevel>().HasData(
                new BadgeLevel { BadgeLevelID = 7, BadgeDefinitionID = badgeDefToDosCompleted.BadgeDefinitionID, /* BadgeDefinition = badgeDefToDosCompleted, */ Level = 1, Name = "Temel Malzemeler", Description = "10 görev tamamla.", IconURL = "https://res.cloudinary.com/dp7utrng4/image/upload/v1748396778/temel_malzemeler_kfuksw.png", RequiredValue = 10 },
                new BadgeLevel { BadgeLevelID = 8, BadgeDefinitionID = badgeDefToDosCompleted.BadgeDefinitionID, /* BadgeDefinition = badgeDefToDosCompleted, */ Level = 2, Name = "Tırmanış Kiti", Description = "50 görev tamamla.", IconURL = "https://res.cloudinary.com/dp7utrng4/image/upload/v1748396779/tirmanis_kiti_n9mg6o.png", RequiredValue = 50 },
                new BadgeLevel { BadgeLevelID = 9, BadgeDefinitionID = badgeDefToDosCompleted.BadgeDefinitionID, /* BadgeDefinition = badgeDefToDosCompleted, */ Level = 3, Name = "Ekspedisyon Hazırlığı", Description = "150 görev tamamla.", IconURL = "https://res.cloudinary.com/dp7utrng4/image/upload/v1748396777/ekspedisyon_hazirligi_l3ysxu.png", RequiredValue = 150 }
            );
        }

        private void SeedStoreItemData(ModelBuilder builder)
        {
            builder.Entity<StoreItem>().HasData(
                new StoreItem
                {
                    StoreItemId = 1,
                    Name = "Keşif Simgesi Alfa",
                    Description = "Profilin için özel bir keşifçi simgesi.",
                    Category = "Kozmetik",
                    PriceSS = 25,
                    IconUrl = "https://res.cloudinary.com/dp7utrng4/image/upload/v1749292205/discovery_badge_alpha_b4ojhb.png",
                    IsConsumable = false,
                    EffectDetails = "{\"effect\": \"profile_icon_unlock\", \"icon_id\": \"alpha_explorer\"}"
                },
                new StoreItem
                {
                    StoreItemId = 2,
                    Name = "Zirve Manzarası Teması",
                    Description = "Uygulama arayüzün için ferahlatıcı bir dağ manzarası teması.",
                    Category = "Kozmetik",
                    PriceSS = 45,
                    IconUrl = "https://res.cloudinary.com/dp7utrng4/image/upload/v1749292204/theme_mountain_peak_rcdpkb.png",
                    IsConsumable = false,
                    EffectDetails = "{\"effect\": \"theme_unlock\", \"theme_id\": \"mountain_peak\"}"
                },
                new StoreItem
                {
                    StoreItemId = 3,
                    Name = "Pusula",
                    Description = "Bir sonraki tamamladığın görev için ekstra +25 Steps kazandırır.",
                    Category = "İşlevsel",
                    PriceSS = 10,
                    IconUrl = "https://res.cloudinary.com/dp7utrng4/image/upload/v1749292205/item_compass_a3wqps.png",
                    IsConsumable = true,
                    MaxQuantityPerUser = 5, // Example: User can hold max 5 compasses
                    EffectDetails = "{\"type\": \"todo_steps_bonus\", \"amount\": 25}"
                },
                new StoreItem
                {
                    StoreItemId = 4,
                    Name = "Enerji Barı",
                    Description = "Bir sonraki odak seansından kazanacağın temel Steps miktarını %15 artırır.",
                    Category = "İşlevsel",
                    PriceSS = 20,
                    IconUrl = "https://res.cloudinary.com/dp7utrng4/image/upload/v1749292205/item_energy_bar_rgtdw2.png",
                    IsConsumable = true,
                    MaxQuantityPerUser = 3, // Example: User can hold max 3 energy bars
                    EffectDetails = "{\"type\": \"session_steps_boost_percentage\", \"amount\": 15}"
                },
                new StoreItem
                {
                    StoreItemId = 5,
                    Name = "Günlük İzin",
                    Description = "Bir günlük serini kaybetmeni önler. Ayda bir kullanılabilir.",
                    Category = "Seri Koruyucular",
                    PriceSS = 120,
                    IconUrl = "https://res.cloudinary.com/dp7utrng4/image/upload/v1749292204/item_streak_shield_ik4aiy.png",
                    IsConsumable = true,
                    MaxQuantityPerUser = 1,
                    EffectDetails = "{\"type\": \"streak_shield\", \"duration_days\": 1}"
                }
            );
        }
    }
}
