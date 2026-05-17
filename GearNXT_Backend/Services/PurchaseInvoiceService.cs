using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GearNXT_Backend.Data;
using GearNXT_Backend.DTOs.Inventory;
using GearNXT_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace GearNXT_Backend.Services;

public class PurchaseInvoiceService
{
    private readonly AppDbContext _db;
    private readonly LowStockNotifier _notifier;

    public PurchaseInvoiceService(AppDbContext db, LowStockNotifier notifier)
    {
        _db = db;
        _notifier = notifier;
    }

    public async Task<ServiceResult<PurchaseInvoiceDto>> CreateInvoiceAsync(PurchaseInvoiceCreateDto dto, string? createdBy)
    {
        // Validate core invoice structure and vendor before touching stock.
        if (dto.Items.Count == 0)
        {
            return ServiceResult<PurchaseInvoiceDto>.Fail(400, "At least one item is required.");
        }

        var vendorExists = await _db.Vendors.AnyAsync(v => v.Id == dto.VendorId);
        if (!vendorExists)
        {
            return ServiceResult<PurchaseInvoiceDto>.Fail(400, "VendorId is invalid.");
        }

        if (dto.Items.Any(i => i.Quantity <= 0 || i.UnitPrice <= 0))
        {
            return ServiceResult<PurchaseInvoiceDto>.Fail(400, "Quantity and UnitPrice must be greater than 0.");
        }

        var duplicatePartIds = dto.Items
            .GroupBy(i => i.PartId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicatePartIds.Count > 0)
        {
            return ServiceResult<PurchaseInvoiceDto>.Fail(400, "Duplicate PartId entries are not allowed in a single invoice.");
        }

        var partIds = dto.Items.Select(i => i.PartId).Distinct().ToList();
        var parts = await _db.Parts.Where(p => partIds.Contains(p.Id)).ToListAsync();
        if (parts.Count != partIds.Count)
        {
            return ServiceResult<PurchaseInvoiceDto>.Fail(404, "One or more parts were not found.");
        }

        // Single transaction keeps invoice, items, and stock updates consistent.
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var invoice = new PurchaseInvoice
        {
            VendorId = dto.VendorId,
            InvoiceDate = dto.InvoiceDate,
            CreatedBy = createdBy,
            TotalAmount = dto.Items.Sum(i => i.Quantity * i.UnitPrice)
        };

        _db.PurchaseInvoices.Add(invoice);
        await _db.SaveChangesAsync();

        var items = new List<PurchaseInvoiceItem>();
        foreach (var item in dto.Items)
        {
            items.Add(new PurchaseInvoiceItem
            {
                InvoiceId = invoice.Id,
                PartId = item.PartId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });

            // Update inventory levels as items are added to the invoice.
            var part = parts.First(p => p.Id == item.PartId);
            part.StockQuantity += item.Quantity;
        }

        _db.PurchaseInvoiceItems.AddRange(items);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        // Re-evaluate low-stock notifications for affected parts only.
        await _notifier.CreateLowStockNotificationsAsync(partIds);

        var response = new PurchaseInvoiceDto
        {
            Id = invoice.Id,
            VendorId = invoice.VendorId,
            TotalAmount = invoice.TotalAmount,
            InvoiceDate = invoice.InvoiceDate,
            CreatedBy = invoice.CreatedBy,
            Items = items.Select(i => new PurchaseInvoiceItemDto
            {
                PartId = i.PartId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        return ServiceResult<PurchaseInvoiceDto>.Ok(response, 201);
    }

    public async Task<ServiceResult<List<PurchaseInvoiceDto>>> GetInvoicesAsync(
        int? vendorId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return ServiceResult<List<PurchaseInvoiceDto>>.Fail(400, "page and pageSize must be greater than 0.");
        }

        var query = _db.PurchaseInvoices
            .AsNoTracking()
            .Include(i => i.Items)
            .AsQueryable();

        if (vendorId.HasValue)
        {
            query = query.Where(i => i.VendorId == vendorId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(i => i.InvoiceDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(i => i.InvoiceDate <= to.Value);
        }

        var invoices = await query
            .OrderByDescending(i => i.InvoiceDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new PurchaseInvoiceDto
            {
                Id = i.Id,
                VendorId = i.VendorId,
                TotalAmount = i.TotalAmount,
                InvoiceDate = i.InvoiceDate,
                CreatedBy = i.CreatedBy,
                Items = i.Items.Select(item => new PurchaseInvoiceItemDto
                {
                    PartId = item.PartId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                }).ToList()
            })
            .ToListAsync();

        return ServiceResult<List<PurchaseInvoiceDto>>.Ok(invoices);
    }

    public async Task<ServiceResult<PurchaseInvoiceDto>> GetInvoiceByIdAsync(int id)
    {
        var invoice = await _db.PurchaseInvoices
            .AsNoTracking()
            .Include(i => i.Items)
            .Where(i => i.Id == id)
            .Select(i => new PurchaseInvoiceDto
            {
                Id = i.Id,
                VendorId = i.VendorId,
                TotalAmount = i.TotalAmount,
                InvoiceDate = i.InvoiceDate,
                CreatedBy = i.CreatedBy,
                Items = i.Items.Select(item => new PurchaseInvoiceItemDto
                {
                    PartId = item.PartId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (invoice == null)
        {
            return ServiceResult<PurchaseInvoiceDto>.Fail(404, "Purchase invoice not found.");
        }

        return ServiceResult<PurchaseInvoiceDto>.Ok(invoice);
    }
}
