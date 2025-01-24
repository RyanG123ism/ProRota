using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProRota.Migrations
{
    /// <inheritdoc />
    public partial class addingSiteConfigurationAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<DateTime>(
                name: "FridayCloseTime",
                table: "Sites",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FridayOpenTime",
                table: "Sites",
                type: "datetime2",
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
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MondayCloseTime",
                table: "Sites",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MondayOpenTime",
                table: "Sites",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfSections",
                table: "Sites",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SaturdayCloseTime",
                table: "Sites",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SaturdayOpenTime",
                table: "Sites",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SundayCloseTime",
                table: "Sites",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SundayOpenTime",
                table: "Sites",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ThursdayCloseTime",
                table: "Sites",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ThursdayOpenTime",
                table: "Sites",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TuesdayCloseTime",
                table: "Sites",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TuesdayOpenTime",
                table: "Sites",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WednesdayCloseTime",
                table: "Sites",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WednesdayOpenTime",
                table: "Sites",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfigurationComplete",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "CoversCapacity",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "FridayCloseTime",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "FridayOpenTime",
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
                name: "MondayCloseTime",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "MondayOpenTime",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "NumberOfSections",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "SaturdayCloseTime",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "SaturdayOpenTime",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "SundayCloseTime",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "SundayOpenTime",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "ThursdayCloseTime",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "ThursdayOpenTime",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "TuesdayCloseTime",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "TuesdayOpenTime",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "WednesdayCloseTime",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "WednesdayOpenTime",
                table: "Sites");
        }
    }
}
