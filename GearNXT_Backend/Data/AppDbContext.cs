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
    }
}