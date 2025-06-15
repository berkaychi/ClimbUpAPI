using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimbUpAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAwardedStepsToBadgeLevels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AwardedSteps",
                table: "BadgeLevels",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "BadgeLevels",
                keyColumn: "BadgeLevelID",
                keyValue: 1,
                column: "AwardedSteps",
                value: 0);

            migrationBuilder.UpdateData(
                table: "BadgeLevels",
                keyColumn: "BadgeLevelID",
                keyValue: 2,
                column: "AwardedSteps",
                value: 0);

            migrationBuilder.UpdateData(
                table: "BadgeLevels",
                keyColumn: "BadgeLevelID",
                keyValue: 3,
                column: "AwardedSteps",
                value: 0);

            migrationBuilder.UpdateData(
                table: "BadgeLevels",
                keyColumn: "BadgeLevelID",
                keyValue: 4,
                column: "AwardedSteps",
                value: 0);

            migrationBuilder.UpdateData(
                table: "BadgeLevels",
                keyColumn: "BadgeLevelID",
                keyValue: 5,
                column: "AwardedSteps",
                value: 0);

            migrationBuilder.UpdateData(
                table: "BadgeLevels",
                keyColumn: "BadgeLevelID",
                keyValue: 6,
                column: "AwardedSteps",
                value: 0);

            migrationBuilder.UpdateData(
                table: "BadgeLevels",
                keyColumn: "BadgeLevelID",
                keyValue: 7,
                column: "AwardedSteps",
                value: 0);

            migrationBuilder.UpdateData(
                table: "BadgeLevels",
                keyColumn: "BadgeLevelID",
                keyValue: 8,
                column: "AwardedSteps",
                value: 0);

            migrationBuilder.UpdateData(
                table: "BadgeLevels",
                keyColumn: "BadgeLevelID",
                keyValue: 9,
                column: "AwardedSteps",
                value: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AwardedSteps",
                table: "BadgeLevels");
        }
    }
}
