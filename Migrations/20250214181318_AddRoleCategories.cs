using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProRota.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoleCategoryId",
                table: "AspNetRoles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RoleCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoles_RoleCategoryId",
                table: "AspNetRoles",
                column: "RoleCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoles_RoleCategories_RoleCategoryId",
                table: "AspNetRoles",
                column: "RoleCategoryId",
                principalTable: "RoleCategories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoles_RoleCategories_RoleCategoryId",
                table: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "RoleCategories");

            migrationBuilder.DropIndex(
                name: "IX_AspNetRoles_RoleCategoryId",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "RoleCategoryId",
                table: "AspNetRoles");
        }
    }
}
