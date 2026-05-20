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

        var wrongVendorPart = parts.FirstOrDefault(p => p.VendorId != dto.VendorId);
        if (wrongVendorPart != null)
        {
            return ServiceResult<PurchaseInvoiceDto>.Fail(
                400,
                $"Part '{wrongVendorPart.Name}' does not belong to the selected vendor.");
        }

        // Single transaction keeps invoice, items, and stock updates consistent.
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var invoiceDate = dto.InvoiceDate;
        if (invoiceDate == default || invoiceDate.Year < 2000)
        {
            invoiceDate = DateTime.UtcNow;
        }

        var invoice = new PurchaseInvoice
        {
            VendorId = dto.VendorId,
            InvoiceDate = invoiceDate,
            CreatedBy = createdBy,
            Status = "Active",
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

        var vendor = await _db.Vendors.AsNoTracking()
            .Where(v => v.Id == invoice.VendorId)
            .Select(v => v.Name)
            .FirstOrDefaultAsync();

        var response = MapToDto(invoice, vendor, items, parts);

        return ServiceResult<PurchaseInvoiceDto>.Ok(response, 201);
    }

    public async Task<ServiceResult<PurchaseInvoiceDto>> CancelInvoiceAsync(int id)
    {
        var invoice = await _db.PurchaseInvoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
        {
            return ServiceResult<PurchaseInvoiceDto>.Fail(404, "Purchase invoice not found.");
        }

        if (string.Equals(invoice.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult<PurchaseInvoiceDto>.Fail(400, "Purchase invoice is already cancelled.");
        }

        var partIds = invoice.Items.Select(i => i.PartId).ToList();
        var parts = await _db.Parts.Where(p => partIds.Contains(p.Id)).ToListAsync();

        await using var transaction = await _db.Database.BeginTransactionAsync();

        foreach (var item in invoice.Items)
        {
            var part = parts.FirstOrDefault(p => p.Id == item.PartId);
            if (part == null)
            {
                await transaction.RollbackAsync();
                return ServiceResult<PurchaseInvoiceDto>.Fail(404, "One or more parts were not found.");
            }

            if (part.StockQuantity < item.Quantity)
            {
                await transaction.RollbackAsync();
                return ServiceResult<PurchaseInvoiceDto>.Fail(400, $"Cannot cancel invoice: insufficient stock to reverse for part {part.Name}.");
            }

            part.StockQuantity -= item.Quantity;
        }

        invoice.Status = "Cancelled";
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        await _notifier.CreateLowStockNotificationsAsync(partIds);

        var vendorName = await _db.Vendors.AsNoTracking()
            .Where(v => v.Id == invoice.VendorId)
            .Select(v => v.Name)
            .FirstOrDefaultAsync();

        var dto = MapToDto(invoice, vendorName, invoice.Items.ToList(), parts);
        return ServiceResult<PurchaseInvoiceDto>.Ok(dto);
    }

    private static PurchaseInvoiceDto MapToDto(
        PurchaseInvoice invoice,
        string? vendorName,
        IEnumerable<PurchaseInvoiceItem> items,
        List<Part> parts)
    {
        return new PurchaseInvoiceDto
        {
            Id = invoice.Id,
            VendorId = invoice.VendorId,
            VendorName = vendorName,
            TotalAmount = invoice.TotalAmount,
            InvoiceDate = invoice.InvoiceDate,
            CreatedBy = invoice.CreatedBy,
            Status = invoice.Status,
            Items = items.Select(i =>
            {
                var part = parts.FirstOrDefault(p => p.Id == i.PartId);
                return new PurchaseInvoiceItemDto
                {
                    PartId = i.PartId,
                    PartName = part?.Name,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                };
            }).ToList()
        };
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
            .ThenInclude(item => item.Part)
            .Include(i => i.Vendor)
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

        var invoiceEntities = await query
            .OrderByDescending(i => i.InvoiceDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var invoices = invoiceEntities.Select(i =>
        {
            var parts = i.Items
                .Where(item => item.Part != null)
                .Select(item => item.Part!)
                .ToList();
            return MapToDto(i, i.Vendor?.Name, i.Items, parts);
        }).ToList();

        return ServiceResult<List<PurchaseInvoiceDto>>.Ok(invoices);
    }

    public async Task<ServiceResult<PurchaseInvoiceDto>> GetInvoiceByIdAsync(int id)
    {
        var invoice = await _db.PurchaseInvoices
            .AsNoTracking()
            .Include(i => i.Items)
            .ThenInclude(item => item.Part)
            .Include(i => i.Vendor)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
        {
            return ServiceResult<PurchaseInvoiceDto>.Fail(404, "Purchase invoice not found.");
        }

        var parts = invoice.Items
            .Where(item => item.Part != null)
            .Select(item => item.Part!)
            .ToList();

        return ServiceResult<PurchaseInvoiceDto>.Ok(
            MapToDto(invoice, invoice.Vendor?.Name, invoice.Items, parts));
    }
}
