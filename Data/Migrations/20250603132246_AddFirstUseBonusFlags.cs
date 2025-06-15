using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimbUpAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFirstUseBonusFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AwardedFirstUseBonus",
                table: "UserTagUsages",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AwardedFirstUseBonus",
                table: "UserSessionTypeUsages",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AwardedFirstUseBonus",
                table: "UserTagUsages");

            migrationBuilder.DropColumn(
                name: "AwardedFirstUseBonus",
                table: "UserSessionTypeUsages");
        }
    }
}
