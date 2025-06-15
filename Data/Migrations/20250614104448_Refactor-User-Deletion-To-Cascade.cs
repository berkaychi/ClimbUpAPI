using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimbUpAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorUserDeletionToCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SessionTypes_AspNetUsers_UserId",
                table: "SessionTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_Tags_AspNetUsers_UserId",
                table: "Tags");

            migrationBuilder.DropForeignKey(
                name: "FK_ToDoItems_AspNetUsers_UserId",
                table: "ToDoItems");

            migrationBuilder.AddForeignKey(
                name: "FK_SessionTypes_AspNetUsers_UserId",
                table: "SessionTypes",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_AspNetUsers_UserId",
                table: "Tags",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ToDoItems_AspNetUsers_UserId",
                table: "ToDoItems",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SessionTypes_AspNetUsers_UserId",
                table: "SessionTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_Tags_AspNetUsers_UserId",
                table: "Tags");

            migrationBuilder.DropForeignKey(
                name: "FK_ToDoItems_AspNetUsers_UserId",
                table: "ToDoItems");

            migrationBuilder.AddForeignKey(
                name: "FK_SessionTypes_AspNetUsers_UserId",
                table: "SessionTypes",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_AspNetUsers_UserId",
                table: "Tags",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ToDoItems_AspNetUsers_UserId",
                table: "ToDoItems",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
