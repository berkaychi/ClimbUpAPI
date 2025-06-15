using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimbUpAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserHiddenSystemEntityTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserHiddenSystemEntities",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", nullable: false),
                    EntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserHiddenSystemEntities", x => new { x.UserId, x.EntityType, x.EntityId });
                    table.ForeignKey(
                        name: "FK_UserHiddenSystemEntities_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "StoreItems",
                keyColumn: "StoreItemId",
                keyValue: 1,
                column: "IconUrl",
                value: "https://res.cloudinary.com/dp7utrng4/image/upload/v1749292205/discovery_badge_alpha_b4ojhb.png");

            migrationBuilder.UpdateData(
                table: "StoreItems",
                keyColumn: "StoreItemId",
                keyValue: 2,
                column: "IconUrl",
                value: "https://res.cloudinary.com/dp7utrng4/image/upload/v1749292204/theme_mountain_peak_rcdpkb.png");

            migrationBuilder.UpdateData(
                table: "StoreItems",
                keyColumn: "StoreItemId",
                keyValue: 3,
                column: "IconUrl",
                value: "https://res.cloudinary.com/dp7utrng4/image/upload/v1749292205/item_compass_a3wqps.png");

            migrationBuilder.UpdateData(
                table: "StoreItems",
                keyColumn: "StoreItemId",
                keyValue: 4,
                column: "IconUrl",
                value: "https://res.cloudinary.com/dp7utrng4/image/upload/v1749292205/item_energy_bar_rgtdw2.png");

            migrationBuilder.UpdateData(
                table: "StoreItems",
                keyColumn: "StoreItemId",
                keyValue: 5,
                columns: new[] { "Description", "IconUrl" },
                values: new object[] { "Bir günlük serini kaybetmeni önler. Ayda bir kullanılabilir.", "https://res.cloudinary.com/dp7utrng4/image/upload/v1749292204/item_streak_shield_ik4aiy.png" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserHiddenSystemEntities");

            migrationBuilder.UpdateData(
                table: "StoreItems",
                keyColumn: "StoreItemId",
                keyValue: 1,
                column: "IconUrl",
                value: "https://res.cloudinary.com/dp7utrng4/image/upload/v1700000001/store_icons/discovery_badge_alpha.png");

            migrationBuilder.UpdateData(
                table: "StoreItems",
                keyColumn: "StoreItemId",
                keyValue: 2,
                column: "IconUrl",
                value: "https://res.cloudinary.com/dp7utrng4/image/upload/v1700000002/store_icons/theme_mountain_peak.png");

            migrationBuilder.UpdateData(
                table: "StoreItems",
                keyColumn: "StoreItemId",
                keyValue: 3,
                column: "IconUrl",
                value: "https://res.cloudinary.com/dp7utrng4/image/upload/v1700000003/store_icons/item_compass.png");

            migrationBuilder.UpdateData(
                table: "StoreItems",
                keyColumn: "StoreItemId",
                keyValue: 4,
                column: "IconUrl",
                value: "https://res.cloudinary.com/dp7utrng4/image/upload/v1700000004/store_icons/item_energy_bar.png");

            migrationBuilder.UpdateData(
                table: "StoreItems",
                keyColumn: "StoreItemId",
                keyValue: 5,
                columns: new[] { "Description", "IconUrl" },
                values: new object[] { "Bir günlük serini kaybetmeni önler. Ayda bir kullanılabilir (bu kural servis katmanında uygulanmalı).", "https://res.cloudinary.com/dp7utrng4/image/upload/v1700000005/store_icons/item_streak_shield.png" });
        }
    }
}
