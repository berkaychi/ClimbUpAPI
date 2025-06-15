using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ClimbUpAPI.Migrations
{
    /// <inheritdoc />
    public partial class RefactorFocusSessionAndTypeModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TaskType = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetProgress = table.Column<int>(type: "INTEGER", nullable: false),
                    Recurrence = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProfilePictureUrl = table.Column<string>(type: "TEXT", nullable: true),
                    EmailConfirmationCode = table.Column<string>(type: "TEXT", nullable: true),
                    EmailConfirmationCodeExpiration = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BadgeDefinitions",
                columns: table => new
                {
                    BadgeDefinitionID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CoreName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MetricToTrack = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadgeDefinitions", x => x.BadgeDefinitionID);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    WorkDuration = table.Column<int>(type: "INTEGER", nullable: false),
                    BreakDuration = table.Column<int>(type: "INTEGER", nullable: true),
                    NumberOfCycles = table.Column<int>(type: "INTEGER", nullable: true),
                    IsSystemDefined = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionTypes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Color = table.Column<string>(type: "TEXT", nullable: false),
                    IsSystemDefined = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tags_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ToDoItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ForDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserIntendedStartTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    AutoCompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ManuallyCompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TargetWorkDuration = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    AccumulatedWorkDuration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToDoItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToDoItems_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserAppTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AppUserId = table.Column<string>(type: "TEXT", nullable: false),
                    AppTaskId = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CurrentProgress = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAppTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAppTasks_AppTasks_AppTaskId",
                        column: x => x.AppTaskId,
                        principalTable: "AppTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAppTasks_AspNetUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Token = table.Column<string>(type: "TEXT", nullable: false),
                    Expires = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Revoked = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRefreshTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserStats",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    TotalFocusDurationSeconds = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalCompletedSessions = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalStartedSessions = table.Column<int>(type: "INTEGER", nullable: false),
                    LongestSingleSessionDurationSeconds = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalToDosCompletedWithFocus = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentStreakDays = table.Column<int>(type: "INTEGER", nullable: false),
                    LongestStreakDays = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalToDosCompleted = table.Column<int>(type: "INTEGER", nullable: false),
                    LastSessionCompletionDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStats", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserStats_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BadgeLevels",
                columns: table => new
                {
                    BadgeLevelID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BadgeDefinitionID = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    IconURL = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    RequiredValue = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadgeLevels", x => x.BadgeLevelID);
                    table.ForeignKey(
                        name: "FK_BadgeLevels_BadgeDefinitions_BadgeDefinitionID",
                        column: x => x.BadgeDefinitionID,
                        principalTable: "BadgeDefinitions",
                        principalColumn: "BadgeDefinitionID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FocusSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CustomDuration = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentStateEndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedCycles = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalWorkDuration = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalBreakDuration = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionTypeId = table.Column<int>(type: "INTEGER", nullable: true),
                    FocusLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    ReflectionNotes = table.Column<string>(type: "TEXT", nullable: true),
                    ToDoItemId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FocusSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FocusSessions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FocusSessions_SessionTypes_SessionTypeId",
                        column: x => x.SessionTypeId,
                        principalTable: "SessionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FocusSessions_ToDoItems_ToDoItemId",
                        column: x => x.ToDoItemId,
                        principalTable: "ToDoItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ToDoTags",
                columns: table => new
                {
                    ToDoItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    TagId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToDoTags", x => new { x.ToDoItemId, x.TagId });
                    table.ForeignKey(
                        name: "FK_ToDoTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ToDoTags_ToDoItems_ToDoItemId",
                        column: x => x.ToDoItemId,
                        principalTable: "ToDoItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserBadges",
                columns: table => new
                {
                    UserBadgeID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    BadgeLevelID = table.Column<int>(type: "INTEGER", nullable: false),
                    DateAchieved = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBadges", x => x.UserBadgeID);
                    table.ForeignKey(
                        name: "FK_UserBadges_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserBadges_BadgeLevels_BadgeLevelID",
                        column: x => x.BadgeLevelID,
                        principalTable: "BadgeLevels",
                        principalColumn: "BadgeLevelID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FocusSessionTags",
                columns: table => new
                {
                    FocusSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    TagId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FocusSessionTags", x => new { x.FocusSessionId, x.TagId });
                    table.ForeignKey(
                        name: "FK_FocusSessionTags_FocusSessions_FocusSessionId",
                        column: x => x.FocusSessionId,
                        principalTable: "FocusSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FocusSessionTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AppTasks",
                columns: new[] { "Id", "Description", "IsActive", "Recurrence", "TargetProgress", "TaskType", "Title" },
                values: new object[,]
                {
                    { 1, "Günlük toplam 50 dakika odaklanma süresine ulaşın.", true, "Daily", 50, 1, "Günlük Odak Hedefi" },
                    { 2, "Haftalık toplam 240 dakika (4 saat) odaklanma süresine ulaşın.", true, "Weekly", 240, 2, "Haftalık Odak Hedefi" }
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "871a497c-21a6-4e0a-a9c7-1e6a6b8f1a7e", null, "Admin", "ADMIN" },
                    { "a1b2c3d4-e5f6-7890-1234-567890abcdef", null, "User", "USER" }
                });

            migrationBuilder.InsertData(
                table: "BadgeDefinitions",
                columns: new[] { "BadgeDefinitionID", "CoreName", "Description", "MetricToTrack" },
                values: new object[,]
                {
                    { 1, "Zirve Akıncısı", "Tamamlanan odak seansı sayısına göre kazanılır.", "completed_focus_sessions" },
                    { 2, "İrtifa Koleksiyoncusu", "Toplam odaklanma süresine göre kazanılır.", "total_focus_duration_hours" },
                    { 3, "Malzeme Depocusu", "Tamamlanan görev sayısına göre kazanılır.", "total_todos_completed" }
                });

            migrationBuilder.InsertData(
                table: "SessionTypes",
                columns: new[] { "Id", "BreakDuration", "Description", "IsActive", "IsSystemDefined", "Name", "NumberOfCycles", "UserId", "WorkDuration" },
                values: new object[,]
                {
                    { 1, 300, null, true, true, "Classic Pomodoro (25/5)", 4, null, 1500 },
                    { 2, 600, null, true, true, "Deep Work", 1, null, 3600 },
                    { 3, 180, null, true, true, "Quick Focus", 1, null, 900 },
                    { 4, 600, null, true, true, "Work Sprint", 2, null, 2700 },
                    { 5, 300, null, true, true, "Focus Sprint (25/5)", null, null, 1500 }
                });

            migrationBuilder.InsertData(
                table: "Tags",
                columns: new[] { "Id", "Color", "Description", "IsArchived", "IsSystemDefined", "Name", "UserId" },
                values: new object[,]
                {
                    { 1, "#FF5733", "Work-related tasks", false, true, "Work", null },
                    { 2, "#FF5733", "Study-related tasks", false, true, "Study", null },
                    { 3, "#FF5733", "Personal tasks and projects", false, true, "Personal", null },
                    { 4, "#FF5733", "High-priority tasks", false, true, "Urgent", null },
                    { 5, "#FF5733", "Significant but not necessarily urgent tasks", false, true, "Important", null }
                });

            migrationBuilder.InsertData(
                table: "BadgeLevels",
                columns: new[] { "BadgeLevelID", "BadgeDefinitionID", "Description", "IconURL", "Level", "Name", "RequiredValue" },
                values: new object[,]
                {
                    { 1, 1, "5 odak seansı tamamla.", "/images/badges/focus_sessions_1.png", 1, "İlk Adım", 5 },
                    { 2, 1, "20 odak seansı tamamla.", "/images/badges/focus_sessions_2.png", 2, "Patika Takipçisi", 20 },
                    { 3, 1, "50 odak seansı tamamla.", "/images/badges/focus_sessions_3.png", 3, "Kaya Tırmanışçısı", 50 },
                    { 4, 2, "Toplam 10 saat odaklan.", "/images/badges/focus_duration_1.png", 1, "Alçak Rakım Gözcüsü", 10 },
                    { 5, 2, "Toplam 50 saat odaklan.", "/images/badges/focus_duration_2.png", 2, "Orta Rakım Kaşifi", 50 },
                    { 6, 2, "Toplam 150 saat odaklan.", "/images/badges/focus_duration_3.png", 3, "Yüksek Rakım Uzmanı", 150 },
                    { 7, 3, "10 görev tamamla.", "/images/badges/todos_completed_1.png", 1, "Temel Malzemeler", 10 },
                    { 8, 3, "50 görev tamamla.", "/images/badges/todos_completed_2.png", 2, "Tırmanış Kiti", 50 },
                    { 9, 3, "150 görev tamamla.", "/images/badges/todos_completed_3.png", 3, "Ekspedisyon Hazırlığı", 150 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BadgeLevels_BadgeDefinitionID",
                table: "BadgeLevels",
                column: "BadgeDefinitionID");

            migrationBuilder.CreateIndex(
                name: "IX_FocusSessions_SessionTypeId",
                table: "FocusSessions",
                column: "SessionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_FocusSessions_ToDoItemId",
                table: "FocusSessions",
                column: "ToDoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_FocusSessions_UserId",
                table: "FocusSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FocusSessionTags_TagId",
                table: "FocusSessionTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionTypes_UserId_Name",
                table: "SessionTypes",
                columns: new[] { "UserId", "Name" },
                unique: true,
                filter: "IsSystemDefined = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_UserId_Name",
                table: "Tags",
                columns: new[] { "UserId", "Name" },
                unique: true,
                filter: "IsSystemDefined = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ToDoItems_UserId",
                table: "ToDoItems",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ToDoTags_TagId",
                table: "ToDoTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAppTasks_AppTaskId",
                table: "UserAppTasks",
                column: "AppTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAppTasks_AppUserId",
                table: "UserAppTasks",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBadges_BadgeLevelID",
                table: "UserBadges",
                column: "BadgeLevelID");

            migrationBuilder.CreateIndex(
                name: "IX_UserBadges_UserId",
                table: "UserBadges",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_UserId",
                table: "UserRefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserStats_TotalCompletedSessions",
                table: "UserStats",
                column: "TotalCompletedSessions");

            migrationBuilder.CreateIndex(
                name: "IX_UserStats_TotalFocusDurationSeconds",
                table: "UserStats",
                column: "TotalFocusDurationSeconds");

            migrationBuilder.CreateIndex(
                name: "IX_UserStats_TotalToDosCompleted",
                table: "UserStats",
                column: "TotalToDosCompleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "FocusSessionTags");

            migrationBuilder.DropTable(
                name: "ToDoTags");

            migrationBuilder.DropTable(
                name: "UserAppTasks");

            migrationBuilder.DropTable(
                name: "UserBadges");

            migrationBuilder.DropTable(
                name: "UserRefreshTokens");

            migrationBuilder.DropTable(
                name: "UserStats");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "FocusSessions");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "AppTasks");

            migrationBuilder.DropTable(
                name: "BadgeLevels");

            migrationBuilder.DropTable(
                name: "SessionTypes");

            migrationBuilder.DropTable(
                name: "ToDoItems");

            migrationBuilder.DropTable(
                name: "BadgeDefinitions");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
