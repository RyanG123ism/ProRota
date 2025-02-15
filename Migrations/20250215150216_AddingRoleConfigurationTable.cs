using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProRota.Migrations
{
    /// <inheritdoc />
    public partial class AddingRoleConfigurationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxBarTenders",
                table: "SiteConfigurations");

            migrationBuilder.DropColumn(
                name: "MaxFrontOfHouse",
                table: "SiteConfigurations");

            migrationBuilder.DropColumn(
                name: "MaxManagement",
                table: "SiteConfigurations");

            migrationBuilder.DropColumn(
                name: "MinManagement",
                table: "SiteConfigurations");

            migrationBuilder.CreateTable(
                name: "RoleConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MinEmployees = table.Column<int>(type: "int", nullable: false),
                    MaxEmployees = table.Column<int>(type: "int", nullable: false),
                    RoleCategoryId = table.Column<int>(type: "int", nullable: false),
                    SiteConfigurationId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleConfigurations_RoleCategories_RoleCategoryId",
                        column: x => x.RoleCategoryId,
                        principalTable: "RoleCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleConfigurations_SiteConfigurations_SiteConfigurationId",
                        column: x => x.SiteConfigurationId,
                        principalTable: "SiteConfigurations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleConfigurations_RoleCategoryId",
                table: "RoleConfigurations",
                column: "RoleCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleConfigurations_SiteConfigurationId",
                table: "RoleConfigurations",
                column: "SiteConfigurationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleConfigurations");

            migrationBuilder.AddColumn<int>(
                name: "MaxBarTenders",
                table: "SiteConfigurations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxFrontOfHouse",
                table: "SiteConfigurations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxManagement",
                table: "SiteConfigurations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinManagement",
                table: "SiteConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
