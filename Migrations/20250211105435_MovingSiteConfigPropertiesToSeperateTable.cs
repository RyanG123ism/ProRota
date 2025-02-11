using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProRota.Migrations
{
    /// <inheritdoc />
    public partial class MovingSiteConfigPropertiesToSeperateTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfigurationComplete",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "CoversCapacity",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "MaxBarTenders",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "MaxFrontOfHouse",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "MaxManagement",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "MinManagement",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "NumberOfSections",
                table: "Sites");

            migrationBuilder.CreateTable(
                name: "SiteConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingDuration = table.Column<TimeSpan>(type: "time", nullable: false),
                    CoversCapacity = table.Column<int>(type: "int", nullable: true),
                    NumberOfSections = table.Column<int>(type: "int", nullable: true),
                    MaxFrontOfHouse = table.Column<int>(type: "int", nullable: true),
                    MaxBarTenders = table.Column<int>(type: "int", nullable: true),
                    MaxManagement = table.Column<int>(type: "int", nullable: true),
                    MinManagement = table.Column<int>(type: "int", nullable: false),
                    SiteId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteConfigurations_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SiteConfigurations_SiteId",
                table: "SiteConfigurations",
                column: "SiteId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteConfigurations");

            migrationBuilder.AddColumn<bool>(
                name: "ConfigurationComplete",
                table: "Sites",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CoversCapacity",
                table: "Sites",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxBarTenders",
                table: "Sites",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxFrontOfHouse",
                table: "Sites",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxManagement",
                table: "Sites",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinManagement",
                table: "Sites",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfSections",
                table: "Sites",
                type: "int",
                nullable: true);
        }
    }
}
