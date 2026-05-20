using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GearNXT_Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class staffseeddata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "IsActive", "Name", "PasswordHash", "Phone", "Role", "UpdatedAt" },
                values: new object[] { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "staff@gearnxt.com", true, "Staff Member", "$2a$11$Sw.RuMV4y9DiJY1wcIM6k.9/yQpmaqNBe3H7uB5Vr/htVWaYKp94i", "9800000001", "Staff", null });

            migrationBuilder.InsertData(
                table: "StaffProfiles",
                columns: new[] { "Id", "EmployeeNumber", "UserId" },
                values: new object[] { 1, "EMP-001", 2 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "StaffProfiles",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2);
        }
    }
}
