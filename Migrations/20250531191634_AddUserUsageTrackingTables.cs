using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimbUpAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddUserUsageTrackingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserSessionTypeUsages",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    SessionTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Score = table.Column<double>(type: "REAL", nullable: false),
                    LastUsedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessionTypeUsages", x => new { x.UserId, x.SessionTypeId });
                    table.ForeignKey(
                        name: "FK_UserSessionTypeUsages_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSessionTypeUsages_SessionTypes_SessionTypeId",
                        column: x => x.SessionTypeId,
                        principalTable: "SessionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTagUsages",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    TagId = table.Column<int>(type: "INTEGER", nullable: false),
                    Score = table.Column<double>(type: "REAL", nullable: false),
                    LastUsedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTagUsages", x => new { x.UserId, x.TagId });
                    table.ForeignKey(
                        name: "FK_UserTagUsages_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserTagUsages_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "BadgeLevels",
                keyColumn: "BadgeLevelID",
                keyValue: 1,
                column: "IconURL",
                value: "https://res.cloudinary.com/dp7utrng4/image/upload/v1748396782/ilk_adim_mifzbj.png");

            migrationBuilder.UpdateData(
                table: "BadgeLevels",
                keyColumn: "BadgeLevelID",
                keyValue: 2,
                column: "IconURL",
                value: "https://res.cloudinary.com/dp7utrng4/image/upload/v1748396777/patika_takipcisi_vn63sd.png");

            migrationBuilder.UpdateData(
                table: "BadgeLevels",
                keyColumn: "BadgeLevelID",
                keyValue: 3,
                column: "IconURL",
                value: "https://res.cloudinary.com/dp7utrng4/image/upload/v1748396778/kaya_tirmaniscisi_qmzbty.png");

            migrationBuilder.UpdateData(
                table: "BadgeLevels",
                keyColumn: "BadgeLevelID",
                keyValue: 4,
                column: "IconURL",
                value: "https://res.cloudinary.com/dp7utrng4/image/upload/v1748396778/alcak_rakim_gozcusu_at9jj1.png");

            migrationBuilder.UpdateData(
                table: "BadgeLevels",
                keyColumn: "BadgeLevelID",
                keyValue: 5,
                column: "IconURL",
                value: "https://res.cloudinary.com/dp7utrng4/image/upload/v1748396777/orta_rakim_kasifi_chunvy.png");

            migrationBuilder.UpdateData(
                table: "BadgeLevels",
                keyColumn: "BadgeLevelID",
                keyValue: 6,
                column: "IconURL",
                value: "https://res.cloudinary.com/dp7utrng4/image/upload/v1748396778/yuksek_rakim_uzmani_dlkob3.png");

            migrationBuilder.UpdateData(
                table: "BadgeLevels",
                keyColumn: "BadgeLevelID",
                keyValue: 7,
                column: "IconURL",
                value: "https://res.cloudinary.com/dp7utrng4/image/upload/v1748396778/temel_malzemeler_kfuksw.png");

            migrationBuilder.UpdateData(
                table: "BadgeLevels",
                keyColumn: "BadgeLevelID",
                keyValue: 8,
                column: "IconURL",
                value: "https://res.cloudinary.com/dp7utrng4/image/upload/v1748396779/tirmanis_kiti_n9mg6o.png");

            migrationBuilder.UpdateData(
                table: "BadgeLevels",
                keyColumn: "BadgeLevelID",
                keyValue: 9,
                column: "IconURL",
                value: "https://res.cloudinary.com/dp7utrng4/image/upload/v1748396777/ekspedisyon_hazirligi_l3ysxu.png");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessionTypeUsages_SessionTypeId",
                table: "UserSessionTypeUsages",
                column: "SessionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTagUsages_TagId",
                table: "UserTagUsages",
                column: "TagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSessionTypeUsages");

            migrationBuilder.DropTable(
                name: "UserTagUsages");

            migrationBuilder.UpdateData(
                table: "BadgeLevels",
                keyColumn: "BadgeLevelID",
                keyValue: 1,
                column: "IconURL",
                value: "/images/badges/focus_sessions_1.png");

            migrationBuilder.UpdateData(
                table: "BadgeLevels",
                keyColumn: "BadgeLevelID",
                keyValue: 2,
                column: "IconURL",
                value: "/images/badges/focus_sessions_2.png");

            migrationBuilder.UpdateData(
                table: "BadgeLevels",
                keyColumn: "BadgeLevelID",
                keyValue: 3,
                column: "IconURL",
                value: "/images/badges/focus_sessions_3.png");

            migrationBuilder.UpdateData(
                table: "BadgeLevels",
                keyColumn: "BadgeLevelID",
                keyValue: 4,
                column: "IconURL",
                value: "/images/badges/focus_duration_1.png");

            migrationBuilder.UpdateData(
                table: "BadgeLevels",
                keyColumn: "BadgeLevelID",
                keyValue: 5,
                column: "IconURL",
                value: "/images/badges/focus_duration_2.png");

            migrationBuilder.UpdateData(
                table: "BadgeLevels",
                keyColumn: "BadgeLevelID",
                keyValue: 6,
                column: "IconURL",
                value: "/images/badges/focus_duration_3.png");

            migrationBuilder.UpdateData(
                table: "BadgeLevels",
                keyColumn: "BadgeLevelID",
                keyValue: 7,
                column: "IconURL",
                value: "/images/badges/todos_completed_1.png");

            migrationBuilder.UpdateData(
                table: "BadgeLevels",
                keyColumn: "BadgeLevelID",
                keyValue: 8,
                column: "IconURL",
                value: "/images/badges/todos_completed_2.png");

            migrationBuilder.UpdateData(
                table: "BadgeLevels",
                keyColumn: "BadgeLevelID",
                keyValue: 9,
                column: "IconURL",
                value: "/images/badges/todos_completed_3.png");
        }
    }
}
