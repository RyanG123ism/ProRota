using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProRota.Migrations
{
    /// <inheritdoc />
    public partial class AddingUserForeignKeyPropertyToCompanyTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Companies",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_ApplicationUserId",
                table: "Companies",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_AspNetUsers_ApplicationUserId",
                table: "Companies",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Companies_AspNetUsers_ApplicationUserId",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_ApplicationUserId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Companies");
        }
    }
}
