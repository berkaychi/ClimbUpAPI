using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimbUpAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGamificationAndStoreSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Stepstones",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "TotalSteps",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "StoreItems",
                columns: table => new
                {
                    StoreItemId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    PriceSS = table.Column<int>(type: "INTEGER", nullable: false),
                    IconUrl = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    IsConsumable = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxQuantityPerUser = table.Column<int>(type: "INTEGER", nullable: true),
                    EffectDetails = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreItems", x => x.StoreItemId);
                });

            migrationBuilder.CreateTable(
                name: "UserStoreItems",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    StoreItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStoreItems", x => new { x.UserId, x.StoreItemId });
                    table.ForeignKey(
                        name: "FK_UserStoreItems_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserStoreItems_StoreItems_StoreItemId",
                        column: x => x.StoreItemId,
                        principalTable: "StoreItems",
                        principalColumn: "StoreItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Stepstones",
                table: "AspNetUsers",
                column: "Stepstones");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_TotalSteps",
                table: "AspNetUsers",
                column: "TotalSteps");

            migrationBuilder.CreateIndex(
                name: "IX_UserStoreItems_StoreItemId",
                table: "UserStoreItems",
                column: "StoreItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserStoreItems");

            migrationBuilder.DropTable(
                name: "StoreItems");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Stepstones",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_TotalSteps",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Stepstones",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TotalSteps",
                table: "AspNetUsers");
        }
    }
}
