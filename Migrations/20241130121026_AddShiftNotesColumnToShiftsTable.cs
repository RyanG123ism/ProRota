using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProRota.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftNotesColumnToShiftsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShiftNotes",
                table: "Shifts",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShiftNotes",
                table: "Shifts");
        }
    }
}
