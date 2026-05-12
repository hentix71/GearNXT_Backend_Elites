using System;
using GearNXT_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace GearNXT_Backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Tables
    public DbSet<User> Users { get; set; }
    public DbSet<Vendor> Vendors { get; set; }
    public DbSet<Part> Parts { get; set; }
    public DbSet<PurchaseInvoice> PurchaseInvoices { get; set; }
    public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<PartRequest> PartRequests { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<CreditPayment> CreditPayments { get; set; }
    public DbSet<SalesInvoice> SalesInvoices { get; set; }
    public DbSet<SalesInvoiceItem> SalesInvoiceItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // =========================
        // USER CONFIGURATION
        // =========================
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();

            entity.Property(u => u.Role)
                  .HasConversion<string>(); // store enum as string
        });

        // =========================
        // VENDOR CONFIGURATION
        // =========================
        modelBuilder.Entity<Vendor>(entity =>
        {
            entity.HasIndex(v => v.Email).IsUnique();
        });


        // =========================
        // PART CONFIGURATION
        // =========================
        modelBuilder.Entity<Part>(entity =>
        {
            entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
            entity.HasOne<Vendor>()
                  .WithMany()
                  .HasForeignKey(p => p.VendorId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // =========================
        // PURCHASE INVOICE CONFIGURATION
        // =========================
        modelBuilder.Entity<PurchaseInvoice>(entity =>
        {
            entity.Property(i => i.TotalAmount).HasColumnType("decimal(18,2)");
            entity.HasMany(i => i.Items)
                  .WithOne()
                  .HasForeignKey(i => i.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PurchaseInvoiceItem>(entity =>
        {
            entity.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
            entity.HasOne<Part>()
                  .WithMany()
                  .HasForeignKey(i => i.PartId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(customer => customer.Email).IsUnique();

            entity.HasMany(customer => customer.Vehicles)
                  .WithOne(vehicle => vehicle.Customer)
                  .HasForeignKey(vehicle => vehicle.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(customer => customer.SalesInvoices)
                  .WithOne(invoice => invoice.Customer)
                  .HasForeignKey(invoice => invoice.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasIndex(vehicle => vehicle.VehicleNumber).IsUnique();
            entity.HasIndex(vehicle => vehicle.LicensePlate).IsUnique();
        });

        modelBuilder.Entity<Part>(entity =>
        {
            entity.HasIndex(part => part.Name).IsUnique();
        });

        modelBuilder.Entity<SalesInvoice>(entity =>
        {
            entity.HasMany(invoice => invoice.Items)
                  .WithOne(item => item.Invoice)
                  .HasForeignKey(item => item.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SalesInvoiceItem>(entity =>
        {
            entity.HasOne(item => item.Part)
                  .WithMany()
                  .HasForeignKey(item => item.PartId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            Name = "Admin",
            Email = "admin@gearnxt.com",
            PasswordHash = "PRECOMPUTED_HASH_HERE", // must be static
            Role = Role.Admin,
            Phone = "9800000000",
            IsActive = true,

            // IMPORTANT: deterministic UTC value (NO UtcNow)
            CreatedAt = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc)
        });



        modelBuilder.Entity<Customer>().HasData(
            new Customer
            {
                Id = 100,
                Name = "Anil Sharma",
                Email = "anil.sharma@example.com",
                Phone = "9801112233",
                Address = "Kathmandu",
                CreatedAt = new DateTime(2026, 04, 02, 0, 0, 0, DateTimeKind.Utc)
            },
            new Customer
            {
                Id = 101,
                Name = "Sita Karki",
                Email = "sita.karki@example.com",
                Phone = "9802223344",
                Address = "Lalitpur",
                CreatedAt = new DateTime(2026, 04, 07, 0, 0, 0, DateTimeKind.Utc)
            }
        );
        modelBuilder.Entity<Vehicle>().HasData(
            new Vehicle
            {
                Id = 1000,
                CustomerId = 100,
                Make = "Honda",
                Model = "City",
                Year = 2022,
                LicensePlate = "BA-2-PA-1234",
                VehicleNumber = "VIN-ANIL-001",
                CreatedAt = new DateTime(2026, 04, 02, 0, 0, 0, DateTimeKind.Utc)
            },
            new Vehicle
            {
                Id = 1001,
                CustomerId = 101,
                Make = "Toyota",
                Model = "Corolla",
                Year = 2021,
                LicensePlate = "BA-3-PA-5678",
                VehicleNumber = "VIN-SITA-001",
                CreatedAt = new DateTime(2026, 04, 07, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        modelBuilder.Entity<Part>().HasData(
            new Part
            {
                Id = 1,
                Name = "Brake Pad Set",
                Description = "Front brake pad set",
                Price = 6800m,
                StockQuantity = 25,
                IsActive = true,
                CreatedAt = new DateTime(2026, 04, 01, 0, 0, 0, DateTimeKind.Utc)
            },
            new Part
            {
                Id = 2,
                Name = "Oil Filter",
                Description = "Premium oil filter",
                Price = 2200m,
                StockQuantity = 40,
                IsActive = true,
                CreatedAt = new DateTime(2026, 04, 01, 0, 0, 0, DateTimeKind.Utc)
            },
            new Part
            {
                Id = 3,
                Name = "Air Filter",
                Description = "Engine air filter",
                Price = 1800m,
                StockQuantity = 35,
                IsActive = true,
                CreatedAt = new DateTime(2026, 04, 01, 0, 0, 0, DateTimeKind.Utc)
            },
            new Part
            {
                Id = 4,
                Name = "Clutch Plate",
                Description = "Heavy-duty clutch plate",
                Price = 9200m,
                StockQuantity = 18,
                IsActive = true,
                CreatedAt = new DateTime(2026, 04, 01, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed appointments, part requests, reviews, credits, and sales invoices
        modelBuilder.Entity<Appointment>().HasData(
            new Appointment { 
                Id = 1, CustomerId = 10, ServiceType = "Full Vehicle Service", 
                PreferredDate = new DateTime(2026, 04, 30, 0, 0, 0, DateTimeKind.Utc),  
                Status = "Upcoming", Notes = "Regular maintenance", 
                CreatedAt = new DateTime(2026, 04, 01, 0, 0, 0, DateTimeKind.Utc)       
            },
            new Appointment { 
                Id = 2, CustomerId = 10, ServiceType = "Brake Inspection", 
                PreferredDate = new DateTime(2026, 04, 12, 0, 0, 0, DateTimeKind.Utc),  
                Status = "Completed", Notes = "Brake pads replaced", 
                CreatedAt = new DateTime(2026, 03, 28, 0, 0, 0, DateTimeKind.Utc)       
            }
        );

        modelBuilder.Entity<PartRequest>().HasData(
            new PartRequest { 
                Id = 1, CustomerId = 10, PartName = "Turbocharger Kit", 
                Description = "OEM preferred", Status = "Pending", 
                CreatedAt = new DateTime(2026, 04, 25, 0, 0, 0, DateTimeKind.Utc)       
            }
        );

        modelBuilder.Entity<Review>().HasData(
            new Review { 
                Id = 1, CustomerId = 10, Rating = 5, Comment = "Excellent service.", 
                CreatedAt = new DateTime(2026, 04, 13, 0, 0, 0, DateTimeKind.Utc)       
            }
        );

        modelBuilder.Entity<SalesInvoice>().HasData(
            new SalesInvoice
            {
                Id = 2000,
                InvoiceNumber = "INV-20260420-0001",
                CustomerId = 100,
                StaffId = 1,
                TotalAmount = 12500m,
                DiscountAmount = 1250m,
                DiscountApplied = true,
                GrandTotal = 11250m,
                PaymentStatus = "Paid",
                InvoiceDate = new DateTime(2026, 04, 20, 0, 0, 0, DateTimeKind.Utc),
                EmailSent = false
            },
            new SalesInvoice { 
                Id = 2, InvoiceNumber = "INV-2026-1002", CustomerId = 11, 
                TotalAmount = 16500m, DiscountAmount = 1650m, DiscountApplied = true, 
                GrandTotal = 14850m, PaymentStatus = "Credit", 
                InvoiceDate = new DateTime(2026, 03, 12, 0, 0, 0, DateTimeKind.Utc)            
            },
            new SalesInvoice
            {
                Id = 2001,
                InvoiceNumber = "INV-20260421-0002",
                CustomerId = 101,
                StaffId = 1,
                TotalAmount = 16500m,
                DiscountAmount = 1650m,
                DiscountApplied = true,
                GrandTotal = 14850m,
                PaymentStatus = "Credit",
                InvoiceDate = new DateTime(2026, 04, 21, 0, 0, 0, DateTimeKind.Utc),
                EmailSent = false
            }
        );

        modelBuilder.Entity<SalesInvoiceItem>().HasData(
            new SalesInvoiceItem
            {
                Id = 3000,
                InvoiceId = 2000,
                PartId = 1,
                Quantity = 1,
                UnitPrice = 6800m,
                PartName = "Brake Pad Set"
            },
            new SalesInvoiceItem
            {
                Id = 3001,
                InvoiceId = 2000,
                PartId = 4,
                Quantity = 1,
                UnitPrice = 9200m,
                PartName = "Clutch Plate"
            },
            new SalesInvoiceItem
            {
                Id = 3002,
                InvoiceId = 2001,
                PartId = 2,
                Quantity = 3,
                UnitPrice = 2200m,
                PartName = "Oil Filter"
            },
            new SalesInvoiceItem
            {
                Id = 3003,
                InvoiceId = 2001,
                PartId = 3,
                Quantity = 2,
                UnitPrice = 1800m,
                PartName = "Air Filter"
            }
        );

        modelBuilder.Entity<Vendor>().HasData(
            new Vendor
            {
                Id = 1,
                Name = "Atlas Auto Supplies",
                Email = "atlas@vendors.com",
                Phone = "9800000011",
                Address = "Kathmandu",
                IsActive = true,
                CreatedAt = new DateTime(2026, 01, 05, 0, 0, 0, DateTimeKind.Utc)
            },
            new Vendor
            {
                Id = 2,
                Name = "Everest Parts Co.",
                Email = "everest@vendors.com",
                Phone = "9800000022",
                Address = "Pokhara",
                IsActive = true,
                CreatedAt = new DateTime(2026, 01, 05, 0, 0, 0, DateTimeKind.Utc)
            },
            new Vendor
            {
                Id = 3,
                Name = "Terai Traders",
                Email = "terai@vendors.com",
                Phone = "9800000033",
                Address = "Biratnagar",
                IsActive = true,
                CreatedAt = new DateTime(2026, 01, 05, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}