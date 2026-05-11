using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GearNXT_Backend.Data;
using GearNXT_Backend.DTOs.Inventory;
using GearNXT_Backend.Models;
using GearNXT_Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GearNXT_Backend.Controllers;

[ApiController]
[Route("api/admin/purchase-invoices")]
public class AdminPurchaseInvoicesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly LowStockNotifier _notifier;

    public AdminPurchaseInvoicesController(AppDbContext db, LowStockNotifier notifier)
    {
        _db = db;
        _notifier = notifier;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminPurchaseInvoiceDto>>> GetInvoices()
    {
        var invoices = await _db.PurchaseInvoiceItems
            .AsNoTracking()
            .Include(i => i)
            .Join(_db.PurchaseInvoices,
                item => item.InvoiceId,
                invoice => invoice.Id,
                (item, invoice) => new { item, invoice })
            .Join(_db.Vendors,
                pair => pair.invoice.VendorId,
                vendor => vendor.Id,
                (pair, vendor) => new { pair.item, pair.invoice, vendor })
            .Join(_db.Parts,
                pair => pair.item.PartId,
                part => part.Id,
                (pair, part) => new AdminPurchaseInvoiceDto
                {
                    Id = pair.invoice.Id,
                    Vendor = pair.vendor.Name,
                    Part = part.Name,
                    Quantity = pair.item.Quantity,
                    UnitCost = pair.item.UnitPrice,
                    Date = pair.invoice.InvoiceDate
                })
            .OrderByDescending(i => i.Date)
            .ToListAsync();

        return Ok(invoices);
    }

    [HttpPost]
    public async Task<ActionResult<AdminPurchaseInvoiceDto>> CreateInvoice([FromBody] AdminPurchaseInvoiceCreateDto dto)
    {
        var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Name == dto.Vendor);
        if (vendor == null)
        {
            return NotFound("Vendor not found.");
        }

        var part = await _db.Parts.FirstOrDefaultAsync(p => p.Name == dto.Part);
        if (part == null)
        {
            return NotFound("Part not found.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var invoice = new PurchaseInvoice
        {
            VendorId = vendor.Id,
            InvoiceDate = dto.Date,
            CreatedBy = "Admin",
            TotalAmount = dto.Quantity * dto.UnitCost
        };

        _db.PurchaseInvoices.Add(invoice);
        await _db.SaveChangesAsync();

        var item = new PurchaseInvoiceItem
        {
            InvoiceId = invoice.Id,
            PartId = part.Id,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitCost
        };

        _db.PurchaseInvoiceItems.Add(item);
        part.StockQty += dto.Quantity;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        await _notifier.CreateLowStockNotificationsAsync();

        return Ok(new AdminPurchaseInvoiceDto
        {
            Id = invoice.Id,
            Vendor = vendor.Name,
            Part = part.Name,
            Quantity = item.Quantity,
            UnitCost = item.UnitPrice,
            Date = invoice.InvoiceDate
        });
    }
}
