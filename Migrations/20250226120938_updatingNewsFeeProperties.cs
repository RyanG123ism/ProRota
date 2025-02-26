using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProRota.Migrations
{
    /// <inheritdoc />
    public partial class updatingNewsFeeProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NewsFeedItems_AspNetUsers_ApplicationUserId",
                table: "NewsFeedItems");

            migrationBuilder.AlterColumn<string>(
                name: "ApplicationUserId",
                table: "NewsFeedItems",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "NewsFeedItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "NewsFeedItems",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SiteId",
                table: "NewsFeedItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetType",
                table: "NewsFeedItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_NewsFeedItems_CompanyId",
                table: "NewsFeedItems",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsFeedItems_CreatedByUserId",
                table: "NewsFeedItems",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsFeedItems_SiteId",
                table: "NewsFeedItems",
                column: "SiteId");

            migrationBuilder.AddForeignKey(
                name: "FK_NewsFeedItems_AspNetUsers_ApplicationUserId",
                table: "NewsFeedItems",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NewsFeedItems_AspNetUsers_CreatedByUserId",
                table: "NewsFeedItems",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NewsFeedItems_Companies_CompanyId",
                table: "NewsFeedItems",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NewsFeedItems_Sites_SiteId",
                table: "NewsFeedItems",
                column: "SiteId",
                principalTable: "Sites",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NewsFeedItems_AspNetUsers_ApplicationUserId",
                table: "NewsFeedItems");

            migrationBuilder.DropForeignKey(
                name: "FK_NewsFeedItems_AspNetUsers_CreatedByUserId",
                table: "NewsFeedItems");

            migrationBuilder.DropForeignKey(
                name: "FK_NewsFeedItems_Companies_CompanyId",
                table: "NewsFeedItems");

            migrationBuilder.DropForeignKey(
                name: "FK_NewsFeedItems_Sites_SiteId",
                table: "NewsFeedItems");

            migrationBuilder.DropIndex(
                name: "IX_NewsFeedItems_CompanyId",
                table: "NewsFeedItems");

            migrationBuilder.DropIndex(
                name: "IX_NewsFeedItems_CreatedByUserId",
                table: "NewsFeedItems");

            migrationBuilder.DropIndex(
                name: "IX_NewsFeedItems_SiteId",
                table: "NewsFeedItems");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "NewsFeedItems");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "NewsFeedItems");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "NewsFeedItems");

            migrationBuilder.DropColumn(
                name: "TargetType",
                table: "NewsFeedItems");

            migrationBuilder.AlterColumn<string>(
                name: "ApplicationUserId",
                table: "NewsFeedItems",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_NewsFeedItems_AspNetUsers_ApplicationUserId",
                table: "NewsFeedItems",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
