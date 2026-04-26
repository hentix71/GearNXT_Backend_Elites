using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GearNXT_Backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBusinessSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete in FK-safe order; includes lines/invoices that reference seeded parts.
            migrationBuilder.Sql(
                """
                DELETE FROM "SalesInvoiceItems"
                WHERE "InvoiceId" IN (2, 2000, 2001)
                   OR "PartId" IN (SELECT "Id" FROM "Parts" WHERE "VendorId" IN (1, 2, 3) OR "Id" IN (1, 2, 3, 4))
                   OR "Id" IN (3000, 3001, 3002, 3003);

                DELETE FROM "SalesInvoices"
                WHERE "Id" IN (2, 2000, 2001)
                   OR "CustomerId" IN (100, 101);

                DELETE FROM "PurchaseInvoiceItems"
                WHERE "PartId" IN (SELECT "Id" FROM "Parts" WHERE "VendorId" IN (1, 2, 3) OR "Id" IN (1, 2, 3, 4))
                   OR "InvoiceId" IN (SELECT "Id" FROM "PurchaseInvoices" WHERE "VendorId" IN (1, 2, 3));

                DELETE FROM "PurchaseInvoices" WHERE "VendorId" IN (1, 2, 3);

                DELETE FROM "Reviews" WHERE "Id" = 1 OR "CustomerId" IN (100, 101);
                DELETE FROM "PartRequests" WHERE "Id" = 1 OR "CustomerId" IN (100, 101);
                DELETE FROM "Appointments" WHERE "Id" IN (1, 2) OR "CustomerId" IN (100, 101);
                DELETE FROM "Vehicles" WHERE "Id" IN (1000, 1001) OR "CustomerId" IN (100, 101);

                DELETE FROM "Parts" WHERE "VendorId" IN (1, 2, 3) OR "Id" IN (1, 2, 3, 4);
                DELETE FROM "Vendors" WHERE "Id" IN (1, 2, 3);

                DELETE FROM "Customers" WHERE "Id" IN (100, 101);

                DELETE FROM "Users" WHERE "Id" IN (100, 101);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "IsActive", "Name", "PasswordHash", "Phone", "Role", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@gearnxt.com", true, "Admin User", "$2a$11$XI1VPFOC221Htz7i0ooIsOzt2JFmDWiM4o4sKGjLTwG4gG0hke0JC", "+977-9810000001", "Admin", null },
                    { 2, new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), "staff@gearnxt.com", true, "Staff User", "$2a$11$V3Gz4YZh0TqU7RUHiE9kIOdNQSBZ67kUcUt5HFNBqNaiiR913WvFe", "+977-9810000002", "Staff", null },
                    { 10, new DateTime(2026, 2, 2, 0, 0, 0, 0, DateTimeKind.Utc), "customer@gearnxt.com", true, "Customer User", "$2a$11$DVS9jdbMM3YvnHOAuaAVUeHr8OHwNHAbRBNfLFW4VbOB0azgp0n/G", "9810000003", "Customer", null },
                    { 100, new DateTime(2026, 4, 2, 0, 0, 0, 0, DateTimeKind.Utc), "anil.sharma@example.com", true, "Anil Sharma", "$2a$11$DVS9jdbMM3YvnHOAuaAVUeHr8OHwNHAbRBNfLFW4VbOB0azgp0n/G", "9801112233", "Customer", null },
                    { 101, new DateTime(2026, 4, 7, 0, 0, 0, 0, DateTimeKind.Utc), "sita.karki@example.com", true, "Sita Karki", "$2a$11$DVS9jdbMM3YvnHOAuaAVUeHr8OHwNHAbRBNfLFW4VbOB0azgp0n/G", "9802223344", "Customer", null }
                });

            migrationBuilder.InsertData(
                table: "Vendors",
                columns: new[] { "Id", "Address", "ContactPerson", "CreatedAt", "Email", "IsActive", "Name", "Phone", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Kathmandu", null, new DateTime(2026, 1, 5, 0, 0, 0, 0, DateTimeKind.Utc), "atlas@vendors.com", true, "Atlas Auto Supplies", "9800000011", null },
                    { 2, "Pokhara", null, new DateTime(2026, 1, 5, 0, 0, 0, 0, DateTimeKind.Utc), "everest@vendors.com", true, "Everest Parts Co.", "9800000022", null },
                    { 3, "Biratnagar", null, new DateTime(2026, 1, 5, 0, 0, 0, 0, DateTimeKind.Utc), "terai@vendors.com", true, "Terai Traders", "9800000033", null }
                });

            migrationBuilder.InsertData(
                table: "AdminProfiles",
                columns: new[] { "Id", "UserId" },
                values: new object[] { 1, 1 });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "CreatedAt", "UserId" },
                values: new object[,]
                {
                    { 10, new DateTime(2026, 2, 2, 0, 0, 0, 0, DateTimeKind.Utc), 10 },
                    { 100, new DateTime(2026, 4, 2, 0, 0, 0, 0, DateTimeKind.Utc), 100 },
                    { 101, new DateTime(2026, 4, 7, 0, 0, 0, 0, DateTimeKind.Utc), 101 }
                });

            migrationBuilder.InsertData(
                table: "Parts",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "IsActive", "Name", "Price", "Sku", "StockQuantity", "VendorId" },
                values: new object[,]
                {
                    { 1, "", new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Front brake pad set", true, "Brake Pad Set", 6800m, null, 25, 1 },
                    { 2, "", new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Premium oil filter", true, "Oil Filter", 2200m, null, 40, 1 },
                    { 3, "", new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Engine air filter", true, "Air Filter", 1800m, null, 35, 2 },
                    { 4, "", new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Heavy-duty clutch plate", true, "Clutch Plate", 9200m, null, 18, 3 }
                });

            migrationBuilder.InsertData(
                table: "StaffProfiles",
                columns: new[] { "Id", "EmployeeNumber", "UserId" },
                values: new object[] { 1, "GNX-STF-0001", 2 });

            migrationBuilder.InsertData(
                table: "Appointments",
                columns: new[] { "Id", "CreatedAt", "CustomerId", "Notes", "PreferredDate", "ServiceType", "Status" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), 100, "Regular maintenance", new DateTime(2026, 4, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Full Vehicle Service", "Upcoming" },
                    { 2, new DateTime(2026, 3, 28, 0, 0, 0, 0, DateTimeKind.Utc), 100, "Brake pads replaced", new DateTime(2026, 4, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Brake Inspection", "Completed" }
                });

            migrationBuilder.InsertData(
                table: "PartRequests",
                columns: new[] { "Id", "CreatedAt", "CustomerId", "Description", "PartName", "StaffComment", "Status", "StatusHistoryJson" },
                values: new object[] { 1, new DateTime(2026, 4, 25, 0, 0, 0, 0, DateTimeKind.Utc), 100, "OEM preferred", "Turbocharger Kit", null, "Pending", null });

            migrationBuilder.InsertData(
                table: "Reviews",
                columns: new[] { "Id", "Comment", "CreatedAt", "CustomerId", "Rating", "Status" },
                values: new object[] { 1, "Excellent service.", new DateTime(2026, 4, 13, 0, 0, 0, 0, DateTimeKind.Utc), 100, 5, "Pending" });

            migrationBuilder.InsertData(
                table: "SalesInvoices",
                columns: new[] { "Id", "CustomerId", "DiscountAmount", "DiscountApplied", "EmailSent", "GrandTotal", "InvoiceDate", "InvoiceNumber", "PaidAmount", "PaymentStatus", "StaffId", "TotalAmount" },
                values: new object[,]
                {
                    { 2, 100, 1650m, true, false, 14850m, new DateTime(2026, 3, 12, 0, 0, 0, 0, DateTimeKind.Utc), "INV-2026-1002", 0m, "Credit", 1, 16500m },
                    { 2000, 100, 1250m, true, false, 11250m, new DateTime(2026, 4, 20, 0, 0, 0, 0, DateTimeKind.Utc), "INV-20260420-0001", 0m, "Paid", 1, 12500m },
                    { 2001, 101, 1650m, true, false, 14850m, new DateTime(2026, 4, 21, 0, 0, 0, 0, DateTimeKind.Utc), "INV-20260421-0002", 0m, "Credit", 1, 16500m }
                });

            migrationBuilder.InsertData(
                table: "Vehicles",
                columns: new[] { "Id", "Color", "CreatedAt", "CustomerId", "FuelType", "LicensePlate", "Make", "Model", "VehicleNumber", "Year" },
                values: new object[,]
                {
                    { 1000, null, new DateTime(2026, 4, 2, 0, 0, 0, 0, DateTimeKind.Utc), 100, null, "BA-2-PA-1234", "Honda", "City", "VIN-ANIL-001", 2022 },
                    { 1001, null, new DateTime(2026, 4, 7, 0, 0, 0, 0, DateTimeKind.Utc), 101, null, "BA-3-PA-5678", "Toyota", "Corolla", "VIN-SITA-001", 2021 }
                });

            migrationBuilder.InsertData(
                table: "SalesInvoiceItems",
                columns: new[] { "Id", "InvoiceId", "PartId", "PartName", "Quantity", "UnitPrice" },
                values: new object[,]
                {
                    { 3000, 2000, 1, "Brake Pad Set", 1, 6800m },
                    { 3001, 2000, 4, "Clutch Plate", 1, 9200m },
                    { 3002, 2001, 2, "Oil Filter", 3, 2200m },
                    { 3003, 2001, 3, "Air Filter", 2, 1800m }
                });
        }
    }
}
