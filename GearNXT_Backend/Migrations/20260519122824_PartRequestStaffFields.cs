using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GearNXT_Backend.Migrations
{
    /// <inheritdoc />
    public partial class PartRequestStaffFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StaffComment",
                table: "PartRequests",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusHistoryJson",
                table: "PartRequests",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PartRequests",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "StaffComment", "StatusHistoryJson" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StaffComment",
                table: "PartRequests");

            migrationBuilder.DropColumn(
                name: "StatusHistoryJson",
                table: "PartRequests");
        }
    }
}
