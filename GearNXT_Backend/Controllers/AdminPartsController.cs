using System;
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
[Route("api/admin/parts")]
public class AdminPartsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly LowStockNotifier _notifier;

    public AdminPartsController(AppDbContext db, LowStockNotifier notifier)
    {
        _db = db;
        _notifier = notifier;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminPartDto>>> GetParts()
    {
        var parts = await _db.Parts
            .AsNoTracking()
            .Select(p => new AdminPartDto
            {
                Id = p.Id,
                Name = p.Name,
                Sku = p.Sku ?? string.Empty,
                Stock = p.StockQuantity,
                Price = p.Price
            })
            .ToListAsync();

        return Ok(parts);
    }

    [HttpPost]
    public async Task<ActionResult<AdminPartDto>> CreatePart([FromBody] AdminPartCreateDto dto)
    {
        var vendorId = dto.VendorId ?? await _db.Vendors.Select(v => v.Id).FirstOrDefaultAsync();
        if (vendorId == 0)
        {
            return BadRequest("No vendor exists to assign this part.");
        }

        var part = new Part
        {
            Name = dto.Name,
            Sku = dto.Sku,
            Category = "General",
            Price = dto.Price,
            StockQuantity = dto.Stock,
            VendorId = vendorId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Parts.Add(part);
        await _db.SaveChangesAsync();

        var response = new AdminPartDto
        {
            Id = part.Id,
            Name = part.Name,
            Sku = part.Sku ?? string.Empty,
            Stock = part.StockQuantity,
            Price = part.Price
        };

        return Ok(response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AdminPartDto>> UpdatePart(int id, [FromBody] AdminPartUpdateDto dto)
    {
        var part = await _db.Parts.FirstOrDefaultAsync(p => p.Id == id);
        if (part == null)
        {
            return NotFound();
        }

        if (dto.VendorId.HasValue)
        {
            var vendorExists = await _db.Vendors.AnyAsync(v => v.Id == dto.VendorId.Value);
            if (!vendorExists)
            {
                return BadRequest("VendorId is invalid.");
            }
            part.VendorId = dto.VendorId.Value;
        }

        part.Name = dto.Name;
        part.Sku = dto.Sku;
        part.StockQuantity = dto.Stock;
        part.Price = dto.Price;

        await _db.SaveChangesAsync();
        await _notifier.CreateLowStockNotificationsAsync();

        return Ok(new AdminPartDto
        {
            Id = part.Id,
            Name = part.Name,
            Sku = part.Sku ?? string.Empty,
            Stock = part.StockQuantity,
            Price = part.Price
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePart(int id)
    {
        var part = await _db.Parts.FirstOrDefaultAsync(p => p.Id == id);
        if (part == null)
        {
            return NotFound();
        }

        _db.Parts.Remove(part);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
