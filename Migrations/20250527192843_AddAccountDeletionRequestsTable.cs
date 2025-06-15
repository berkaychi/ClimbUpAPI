using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimbUpAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountDeletionRequestsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountDeletionRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Token = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ConfirmationDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountDeletionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountDeletionRequests_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountDeletionRequests_Token",
                table: "AccountDeletionRequests",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountDeletionRequests_UserId",
                table: "AccountDeletionRequests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountDeletionRequests");
        }
    }
}
