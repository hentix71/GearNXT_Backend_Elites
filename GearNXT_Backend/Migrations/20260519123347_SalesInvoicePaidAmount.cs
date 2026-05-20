using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GearNXT_Backend.Migrations
{
    /// <inheritdoc />
    public partial class SalesInvoicePaidAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "SalesInvoices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "SalesInvoices",
                keyColumn: "Id",
                keyValue: 2,
                column: "PaidAmount",
                value: 0m);

            migrationBuilder.UpdateData(
                table: "SalesInvoices",
                keyColumn: "Id",
                keyValue: 2000,
                column: "PaidAmount",
                value: 0m);

            migrationBuilder.UpdateData(
                table: "SalesInvoices",
                keyColumn: "Id",
                keyValue: 2001,
                column: "PaidAmount",
                value: 0m);

            migrationBuilder.Sql(
                """
                UPDATE "SalesInvoices"
                SET "PaidAmount" = "GrandTotal"
                WHERE "PaymentStatus" = 'Paid';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "SalesInvoices");
        }
    }
}
