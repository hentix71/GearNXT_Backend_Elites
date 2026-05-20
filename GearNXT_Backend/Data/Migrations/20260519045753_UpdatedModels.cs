using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GearNXT_Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_Email",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Customers");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalDiscountEarned",
                table: "Customers",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalSpent",
                table: "Customers",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "IsActive", "Name", "PasswordHash", "Phone", "Role" },
                values: new object[,]
                {
                    { 100, new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc), "anil.sharma@example.com", true, "Anil Sharma", "$2a$11$Sw.RuMV4y9DiJY1wcIM6k.9/yQpmaqNBe3H7uB5Vr/htVWaYKp94i", "9801112233", "Customer" },
                    { 101, new DateTime(2026, 4, 7, 0, 0, 0, DateTimeKind.Utc), "sita.karki@example.com", true, "Sita Karki", "$2a$11$Sw.RuMV4y9DiJY1wcIM6k.9/yQpmaqNBe3H7uB5Vr/htVWaYKp94i", "9802223344", "Customer" }
                });

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: 100,
                column: "UserId",
                value: 100);

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: 101,
                column: "UserId",
                value: 101);

            migrationBuilder.CreateIndex(
                name: "IX_StaffProfiles_EmployeeNumber",
                table: "StaffProfiles",
                column: "EmployeeNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StaffProfiles_EmployeeNumber",
                table: "StaffProfiles");

            migrationBuilder.DropColumn(
                name: "TotalDiscountEarned",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TotalSpent",
                table: "Customers");

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 100);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Customers",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Customers",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Customers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Customers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: 100,
                columns: new[] { "Address", "Email", "Name", "Phone", "UserId" },
                values: new object[] { "Kathmandu", "anil.sharma@example.com", "Anil Sharma", "9801112233", null });

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: 101,
                columns: new[] { "Address", "Email", "Name", "Phone", "UserId" },
                values: new object[] { "Lalitpur", "sita.karki@example.com", "Sita Karki", "9802223344", null });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Email",
                table: "Customers",
                column: "Email",
                unique: true);
        }
    }
}
