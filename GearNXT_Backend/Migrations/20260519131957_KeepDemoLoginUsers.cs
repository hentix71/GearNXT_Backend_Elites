using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GearNXT_Backend.Migrations
{
    /// <inheritdoc />
    public partial class KeepDemoLoginUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            const string adminHash = "$2a$11$XI1VPFOC221Htz7i0ooIsOzt2JFmDWiM4o4sKGjLTwG4gG0hke0JC";
            const string staffHash = "$2a$11$V3Gz4YZh0TqU7RUHiE9kIOdNQSBZ67kUcUt5HFNBqNaiiR913WvFe";
            const string customerHash = "$2a$11$DVS9jdbMM3YvnHOAuaAVUeHr8OHwNHAbRBNfLFW4VbOB0azgp0n/G";

            migrationBuilder.Sql(
                $"""
                INSERT INTO "Users" ("Id", "Name", "Email", "PasswordHash", "Role", "Phone", "IsActive", "CreatedAt")
                SELECT 1, 'Admin User', 'admin@gearnxt.com', '{adminHash}', 'Admin', '+977-9810000001', TRUE, TIMESTAMPTZ '2026-01-01T00:00:00Z'
                WHERE NOT EXISTS (SELECT 1 FROM "Users" WHERE LOWER("Email") = 'admin@gearnxt.com');

                INSERT INTO "Users" ("Id", "Name", "Email", "PasswordHash", "Role", "Phone", "IsActive", "CreatedAt")
                SELECT 2, 'Staff User', 'staff@gearnxt.com', '{staffHash}', 'Staff', '+977-9810000002', TRUE, TIMESTAMPTZ '2026-01-15T00:00:00Z'
                WHERE NOT EXISTS (SELECT 1 FROM "Users" WHERE LOWER("Email") = 'staff@gearnxt.com');

                INSERT INTO "Users" ("Id", "Name", "Email", "PasswordHash", "Role", "Phone", "IsActive", "CreatedAt")
                SELECT 10, 'Customer User', 'customer@gearnxt.com', '{customerHash}', 'Customer', '9810000003', TRUE, TIMESTAMPTZ '2026-02-02T00:00:00Z'
                WHERE NOT EXISTS (SELECT 1 FROM "Users" WHERE LOWER("Email") = 'customer@gearnxt.com');

                INSERT INTO "AdminProfiles" ("Id", "UserId")
                SELECT 1, u."Id"
                FROM "Users" u
                WHERE LOWER(u."Email") = 'admin@gearnxt.com'
                  AND NOT EXISTS (SELECT 1 FROM "AdminProfiles" ap WHERE ap."UserId" = u."Id");

                INSERT INTO "StaffProfiles" ("Id", "UserId", "EmployeeNumber")
                SELECT 1, u."Id", 'GNX-STF-0001'
                FROM "Users" u
                WHERE LOWER(u."Email") = 'staff@gearnxt.com'
                  AND NOT EXISTS (SELECT 1 FROM "StaffProfiles" sp WHERE sp."UserId" = u."Id");

                INSERT INTO "Customers" ("Id", "UserId", "CreatedAt", "TotalDiscountEarned", "TotalSpent")
                SELECT 10, u."Id", TIMESTAMPTZ '2026-02-02T00:00:00Z', 0, 0
                FROM "Users" u
                WHERE LOWER(u."Email") = 'customer@gearnxt.com'
                  AND NOT EXISTS (SELECT 1 FROM "Customers" c WHERE c."UserId" = u."Id");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Demo users are required for the app; Down is intentionally a no-op.
        }
    }
}
