using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GearNXT_Backend.Data;
using GearNXT_Backend.DTOs.Inventory;
using GearNXT_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace GearNXT_Backend.Services;

public class PartsService
{
    // Shared validation rules for create/update to keep API behavior consistent.
    private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Engine",
        "Brakes",
        "Electrical",
        "Suspension",
        "Transmission",
        "Exhaust",
        "Body",
        "Interior",
        "Lighting",
        "Tires",
        "Fluids",
        "General"
    };

    private readonly AppDbContext _db;
    private readonly LowStockNotifier _notifier;

    public PartsService(AppDbContext db, LowStockNotifier notifier)
    {
        _db = db;
        _notifier = notifier;
    }

    public async Task<ServiceResult<Part>> CreatePartAsync(PartCreateDto dto)
    {
        // Validate required fields and vendor ownership before insert.
        var validationError = ValidatePart(dto.Name, dto.Category, dto.Price, dto.StockQuantity, dto.VendorId);
        if (validationError != null)
        {
            return ServiceResult<Part>.Fail(400, validationError);
        }

        var vendorExists = await _db.Vendors.AnyAsync(v => v.Id == dto.VendorId);
        if (!vendorExists)
        {
            return ServiceResult<Part>.Fail(400, "VendorId is invalid.");
        }

        var normalizedName = dto.Name.Trim().ToLower();
        // Avoid duplicate names for the same vendor.
        var duplicateExists = await _db.Parts
            .AnyAsync(p => p.VendorId == dto.VendorId && p.Name.ToLower() == normalizedName);
        if (duplicateExists)
        {
            return ServiceResult<Part>.Fail(409, "Part already exists for this vendor.");
        }

        var part = new Part
        {
            Name = dto.Name.Trim(),
            Category = dto.Category.Trim(),
            Description = dto.Description,
            Price = dto.Price,
            StockQuantity = dto.StockQuantity,
            VendorId = dto.VendorId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Parts.Add(part);
        await _db.SaveChangesAsync();

        return ServiceResult<Part>.Ok(part, 201);
    }

    public async Task<ServiceResult<List<Part>>> GetPartsAsync(string? category, int? vendorId, string? search)
    {
        var query = _db.Parts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category.ToLower() == category.Trim().ToLower());
        }

        if (vendorId.HasValue)
        {
            query = query.Where(p => p.VendorId == vendorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term));
        }

        var parts = await query.ToListAsync();
        return ServiceResult<List<Part>>.Ok(parts);
    }

    public async Task<ServiceResult<Part>> GetPartByIdAsync(int id)
    {
        var part = await _db.Parts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (part == null)
        {
            return ServiceResult<Part>.Fail(404, "Part not found.");
        }

        return ServiceResult<Part>.Ok(part);
    }

    public async Task<ServiceResult<Part>> UpdatePartAsync(int id, PartUpdateDto dto)
    {
        // Reuse validation and vendor checks for updates.
        var validationError = ValidatePart(dto.Name, dto.Category, dto.Price, dto.StockQuantity, dto.VendorId);
        if (validationError != null)
        {
            return ServiceResult<Part>.Fail(400, validationError);
        }

        var part = await _db.Parts.FirstOrDefaultAsync(p => p.Id == id);
        if (part == null)
        {
            return ServiceResult<Part>.Fail(404, "Part not found.");
        }

        var vendorExists = await _db.Vendors.AnyAsync(v => v.Id == dto.VendorId);
        if (!vendorExists)
        {
            return ServiceResult<Part>.Fail(400, "VendorId is invalid.");
        }

        var normalizedName = dto.Name.Trim().ToLower();
        // Prevent renaming into an existing part for the same vendor.
        var duplicateExists = await _db.Parts
            .AnyAsync(p => p.Id != id && p.VendorId == dto.VendorId && p.Name.ToLower() == normalizedName);
        if (duplicateExists)
        {
            return ServiceResult<Part>.Fail(409, "Part already exists for this vendor.");
        }

        part.Name = dto.Name.Trim();
        part.Category = dto.Category.Trim();
        part.Description = dto.Description;
        part.Price = dto.Price;
        part.StockQuantity = dto.StockQuantity;
        part.VendorId = dto.VendorId;

        await _db.SaveChangesAsync();
        // Trigger low-stock evaluation for the updated part.
        await _notifier.CreateLowStockNotificationsAsync(new[] { part.Id });

        return ServiceResult<Part>.Ok(part);
    }

    public async Task<ServiceResult<object>> DeletePartAsync(int id)
    {
        var part = await _db.Parts.FirstOrDefaultAsync(p => p.Id == id);
        if (part == null)
        {
            return ServiceResult<object>.Fail(404, "Part not found.");
        }

        var hasPurchaseItems = await _db.PurchaseInvoiceItems.AnyAsync(i => i.PartId == id);
        if (hasPurchaseItems)
        {
            return ServiceResult<object>.Fail(409, "Cannot delete part referenced by purchase invoices.");
        }

        _db.Parts.Remove(part);
        await _db.SaveChangesAsync();

        return ServiceResult<object>.Ok(new { ok = true }, 204);
    }

    public async Task<ServiceResult<List<Part>>> GetLowStockPartsAsync()
    {
        var parts = await _db.Parts
            .AsNoTracking()
            .Where(p => p.StockQuantity < 10)
            .ToListAsync();

        return ServiceResult<List<Part>>.Ok(parts);
    }

    private static string? ValidatePart(string name, string category, decimal price, int stockQuantity, int vendorId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Name is required.";
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            return "Category is required.";
        }

        if (!AllowedCategories.Contains(category.Trim()))
        {
            return "Category is invalid.";
        }

        if (price <= 0)
        {
            return "Price must be greater than 0.";
        }

        if (stockQuantity < 0)
        {
            return "StockQuantity must be 0 or greater.";
        }

        if (vendorId <= 0)
        {
            return "VendorId is required.";
        }

        return null;
    }
}
