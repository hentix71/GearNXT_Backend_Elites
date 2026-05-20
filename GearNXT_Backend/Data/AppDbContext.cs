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
    public DbSet<StaffProfile> StaffProfiles { get; set; }
    public DbSet<AdminProfile> AdminProfiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Model Configurations

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();

            entity.Property(u => u.Role)
                  .HasConversion<string>(); // store enum as string
        });

        // Vendor
        modelBuilder.Entity<Vendor>(entity =>
        {
            entity.HasIndex(v => v.Email).IsUnique();
        });

        // Part
        modelBuilder.Entity<Part>(entity =>
        {
            entity.HasIndex(part => part.Name).IsUnique();
            entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
            entity.HasOne(p => p.Vendor)
                .WithMany(v => v.Parts)
                .HasForeignKey(p => p.VendorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // PurchaseInvoice 
        modelBuilder.Entity<PurchaseInvoice>(entity =>
        {
            entity.Property(i => i.TotalAmount).HasColumnType("decimal(18,2)");
            entity.HasOne(pi => pi.Vendor)
                .WithMany(v => v.PurchaseInvoices)
                .HasForeignKey(pi => pi.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(i => i.Items)
                .WithOne(i => i.Invoice)
                .HasForeignKey(i => i.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PurchaseInvoiceItem
        modelBuilder.Entity<PurchaseInvoiceItem>(entity =>
        {
            entity.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
            entity.HasOne(i => i.Part)
                .WithMany(p => p.PurchaseInvoiceItems)
                .HasForeignKey(i => i.PartId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Customer (thin profile linked to User)
        modelBuilder.Entity<Customer>(entity =>
        {
            // No longer store name/email/phone on Customer — use User.
            entity.HasIndex(customer => customer.UserId).IsUnique();

            entity.HasOne(customer => customer.User)
                .WithOne(user => user.Customer)
                .HasForeignKey<Customer>(customer => customer.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(customer => customer.Vehicles)
                  .WithOne(vehicle => vehicle.Customer)
                  .HasForeignKey(vehicle => vehicle.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Monetary aggregates mapping
            entity.Property(c => c.TotalDiscountEarned).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            entity.Property(c => c.TotalSpent).HasColumnType("decimal(18,2)").HasDefaultValue(0m);

            entity.HasMany(customer => customer.SalesInvoices)
                  .WithOne(invoice => invoice.Customer)
                  .HasForeignKey(invoice => invoice.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasMany(customer => customer.Appointments)
                .WithOne(a => a.Customer)
                .HasForeignKey(a => a.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(customer => customer.PartRequests)
                .WithOne(pr => pr.Customer)
                .HasForeignKey(pr => pr.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(customer => customer.Reviews)
                .WithOne(r => r.Customer)
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // StaffProfile
        modelBuilder.Entity<StaffProfile>(entity =>
        {
            entity.HasIndex(s => s.UserId).IsUnique();

            // Employee number (human-facing) should be unique when present
            entity.Property(s => s.EmployeeNumber).HasMaxLength(50);
            entity.HasIndex(s => s.EmployeeNumber).IsUnique();

            entity.HasOne(s => s.User)
                .WithOne(u => u.StaffProfile)
                .HasForeignKey<StaffProfile>(s => s.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // AdminProfile
        modelBuilder.Entity<AdminProfile>(entity =>
        {
            entity.HasIndex(a => a.UserId).IsUnique();

            entity.HasOne(a => a.User)
                  .WithOne(u => u.AdminProfile)
                  .HasForeignKey<AdminProfile>(a => a.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Vehicle
        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasIndex(vehicle => vehicle.VehicleNumber).IsUnique();
            entity.HasIndex(vehicle => vehicle.LicensePlate).IsUnique();
        });

        // SalesInvoice
        modelBuilder.Entity<SalesInvoice>(entity =>
        {
            entity.Property(si => si.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(si => si.DiscountAmount).HasColumnType("decimal(18,2)");
            entity.Property(si => si.GrandTotal).HasColumnType("decimal(18,2)");

            entity.HasMany(invoice => invoice.Items)
                  .WithOne(item => item.Invoice)
                  .HasForeignKey(item => item.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(si => si.Customer)
                .WithMany(c => c.SalesInvoices)
                .HasForeignKey(si => si.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(si => si.Staff)
                .WithMany()
                .HasForeignKey(si => si.StaffId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // SalesInvoiceItem
        modelBuilder.Entity<SalesInvoiceItem>(entity =>
        {
            entity.Property(sii => sii.UnitPrice).HasColumnType("decimal(18,2)");

            entity.HasOne(item => item.Invoice)
                .WithMany(si => si.Items)
                .HasForeignKey(item => item.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(item => item.Part)
                .WithMany(p => p.SalesInvoiceItems)
                .HasForeignKey(item => item.PartId)
                .OnDelete(DeleteBehavior.Restrict);
        });



        // Data Seeding

        // user
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            Name = "Admin",
            Email = "admin@gearnxt.com",
            PasswordHash = "$2a$11$Sw.RuMV4y9DiJY1wcIM6k.9/yQpmaqNBe3H7uB5Vr/htVWaYKp94i", 
            Role = Role.Admin,
            Phone = "9800000000",
            IsActive = true,

            // IMPORTANT: deterministic UTC value (NO UtcNow)
            CreatedAt = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc)
        },
            new User
            {
                Id = 2,
                Name = "Staff Member",
                Email = "staff@gearnxt.com",
                PasswordHash = "$2a$11$Sw.RuMV4y9DiJY1wcIM6k.9/yQpmaqNBe3H7uB5Vr/htVWaYKp94i",
                Role = Role.Staff,
                Phone = "9800000001",
                IsActive = true,
                CreatedAt = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed AdminProfile
        modelBuilder.Entity<AdminProfile>().HasData(new AdminProfile
        {
            Id = 1,
            UserId = 1
        });

        // staff profile
        modelBuilder.Entity<StaffProfile>().HasData(new StaffProfile
        {
            Id = 1,
            UserId = 2,
            EmployeeNumber = "EMP-001"
        });

        // customers (thin profile linked to User)
        modelBuilder.Entity<Customer>().HasData(
            new Customer
            {
                Id = 100,
                UserId = 100,
                CreatedAt = new DateTime(2026, 04, 02, 0, 0, 0, DateTimeKind.Utc),
                TotalDiscountEarned = 0.00m,
                TotalSpent = 0.00m
            },
            new Customer
            {
                Id = 101,
                UserId = 101,
                CreatedAt = new DateTime(2026, 04, 07, 0, 0, 0, DateTimeKind.Utc),
                TotalDiscountEarned = 0.00m,
                TotalSpent = 0.00m
            }
        );

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 100,
                Name = "Anil Sharma",
                Email = "anil.sharma@example.com",
                PasswordHash = "$2a$11$Sw.RuMV4y9DiJY1wcIM6k.9/yQpmaqNBe3H7uB5Vr/htVWaYKp94i",
                Role = Role.Customer,
                Phone = "9801112233",
                IsActive = true,
                CreatedAt = new DateTime(2026, 04, 02, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id = 101,
                Name = "Sita Karki",
                Email = "sita.karki@example.com",
                PasswordHash = "$2a$11$Sw.RuMV4y9DiJY1wcIM6k.9/yQpmaqNBe3H7uB5Vr/htVWaYKp94i",
                Role = Role.Customer,
                Phone = "9802223344",
                IsActive = true,
                CreatedAt = new DateTime(2026, 04, 07, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // vehicles
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
        
        // vendors
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

        // parts
        modelBuilder.Entity<Part>().HasData(
            new Part
            {
                Id = 1,
                Name = "Brake Pad Set",
                Description = "Front brake pad set",
                Price = 6800m,
                StockQuantity = 25,
                VendorId = 1,
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
                VendorId = 1,
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
                VendorId = 2,
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
                VendorId = 3,
                IsActive = true,
                CreatedAt = new DateTime(2026, 04, 01, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // appointments
        modelBuilder.Entity<Appointment>().HasData(
            new Appointment { 
                Id = 1, 
                CustomerId = 100, 
                ServiceType = "Full Vehicle Service", 
                PreferredDate = new DateTime(2026, 04, 30, 0, 0, 0, DateTimeKind.Utc),  
                Status = "Upcoming", 
                Notes = "Regular maintenance", 
                CreatedAt = new DateTime(2026, 04, 01, 0, 0, 0, DateTimeKind.Utc)       
            },
            new Appointment { 
                Id = 2, 
                CustomerId = 100, 
                ServiceType = "Brake Inspection", 
                PreferredDate = new DateTime(2026, 04, 12, 0, 0, 0, DateTimeKind.Utc),  
                Status = "Completed", 
                Notes = "Brake pads replaced", 
                CreatedAt = new DateTime(2026, 03, 28, 0, 0, 0, DateTimeKind.Utc)       
            }
        );

        // part requests
        modelBuilder.Entity<PartRequest>().HasData(
            new PartRequest { 
                Id = 1, 
                CustomerId = 100, 
                PartName = "Turbocharger Kit", 
                Description = "OEM preferred", 
                Status = "Pending", 
                CreatedAt = new DateTime(2026, 04, 25, 0, 0, 0, DateTimeKind.Utc)       
            }
        );

        // reviews
        modelBuilder.Entity<Review>().HasData(
            new Review { 
                Id = 1, 
                CustomerId = 100, 
                Rating = 5, 
                Comment = "Excellent service.", 
                CreatedAt = new DateTime(2026, 04, 13, 0, 0, 0, DateTimeKind.Utc)       
            }
        );

        // sales invoices
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
                Id = 2, 
                InvoiceNumber = "INV-2026-1002", 
                CustomerId = 100, 
                StaffId = 1,
                TotalAmount = 16500m, 
                DiscountAmount = 1650m, 
                DiscountApplied = true, 
                GrandTotal = 14850m, 
                PaymentStatus = "Credit", 
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

        // sales invoice items
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
    }
}