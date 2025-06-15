using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimbUpAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountSettingsFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceBrowserInfo",
                table: "UserRefreshTokens",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "UserRefreshTokens",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUsedDate",
                table: "UserRefreshTokens",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingEmailChangeToken",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PendingEmailChangeTokenExpiration",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingNewEmail",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceBrowserInfo",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "LastUsedDate",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "PendingEmailChangeToken",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PendingEmailChangeTokenExpiration",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PendingNewEmail",
                table: "AspNetUsers");
        }
    }
}
