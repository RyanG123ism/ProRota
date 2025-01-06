using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProRota.Migrations
{
    /// <inheritdoc />
    public partial class addingcolumnstoUserandTimeOffRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "TimeOffRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "HolidaysPerYear",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RemainingHolidays",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "TimeOffRequests");

            migrationBuilder.DropColumn(
                name: "HolidaysPerYear",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RemainingHolidays",
                table: "AspNetUsers");
        }
    }
}
