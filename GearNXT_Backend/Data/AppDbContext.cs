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
    public DbSet<Part> Parts { get; set; }
    public DbSet<PurchaseInvoice> PurchaseInvoices { get; set; }
    public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<PartRequest> PartRequests { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<CreditPayment> CreditPayments { get; set; }
    public DbSet<SalesInvoice> SalesInvoices { get; set; }

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

        // =========================
        // SEED DATA (FIXED - STABLE)
        // =========================
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

        // Seed a couple of customers for development
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 10,
                Name = "Demo Customer 1",
                Email = "customer1@gearnxt.com",
                PasswordHash = "DEMO_HASH",
                Role = Role.Customer,
                Phone = "9810000003",
                IsActive = true,
                CreatedAt = new DateTime(2026, 02, 02, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id = 11,
                Name = "Demo Customer 2",
                Email = "customer2@gearnxt.com",
                PasswordHash = "DEMO_HASH",
                Role = Role.Customer,
                Phone = "9810000004",
                IsActive = true,
                CreatedAt = new DateTime(2026, 03, 01, 0, 0, 0, DateTimeKind.Utc)
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
            new SalesInvoice { 
                Id = 1, InvoiceNumber = "INV-2026-1001", CustomerId = 10, 
                Subtotal = 12500m, Discount = 1250m, DiscountApplied = true, 
                GrandTotal = 11250m, PaymentStatus = "Paid", 
                Date = new DateTime(2026, 04, 20, 0, 0, 0, DateTimeKind.Utc)            
            },
            new SalesInvoice { 
                Id = 2, InvoiceNumber = "INV-2026-1002", CustomerId = 11, 
                Subtotal = 16500m, Discount = 1650m, DiscountApplied = true, 
                GrandTotal = 14850m, PaymentStatus = "Credit", 
                Date = new DateTime(2026, 03, 12, 0, 0, 0, DateTimeKind.Utc)            
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