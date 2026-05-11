using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GearNXT_Backend.Data;
using GearNXT_Backend.DTOs.Inventory;
using GearNXT_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GearNXT_Backend.Controllers;

[ApiController]
[Route("api/admin/vendors")]
public class AdminVendorsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminVendorsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminVendorDto>>> GetVendors()
    {
        var vendors = await _db.Vendors
            .AsNoTracking()
            .Select(v => new AdminVendorDto
            {
                Id = v.Id,
                Name = v.Name,
                Contact = v.Phone ?? string.Empty,
                Status = v.IsActive ? "Active" : "Review",
                Spend = "NPR 0"
            })
            .ToListAsync();

        return Ok(vendors);
    }

    [HttpPost]
    public async Task<ActionResult<AdminVendorDto>> CreateVendor([FromBody] AdminVendorUpsertDto dto)
    {
        var vendor = new Vendor
        {
            Name = dto.Name,
            Phone = dto.Contact,
            IsActive = dto.Status.Equals("Active", StringComparison.OrdinalIgnoreCase)
        };

        _db.Vendors.Add(vendor);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetVendors), new { id = vendor.Id }, new AdminVendorDto
        {
            Id = vendor.Id,
            Name = vendor.Name,
            Contact = vendor.Phone ?? string.Empty,
            Status = vendor.IsActive ? "Active" : "Review",
            Spend = FormatSpend(dto.Spend)
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AdminVendorDto>> UpdateVendor(int id, [FromBody] AdminVendorUpsertDto dto)
    {
        var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == id);
        if (vendor == null)
        {
            return NotFound();
        }

        vendor.Name = dto.Name;
        vendor.Phone = dto.Contact;
        vendor.IsActive = dto.Status.Equals("Active", StringComparison.OrdinalIgnoreCase);

        await _db.SaveChangesAsync();

        return Ok(new AdminVendorDto
        {
            Id = vendor.Id,
            Name = vendor.Name,
            Contact = vendor.Phone ?? string.Empty,
            Status = vendor.IsActive ? "Active" : "Review",
            Spend = FormatSpend(dto.Spend)
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteVendor(int id)
    {
        var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == id);
        if (vendor == null)
        {
            return NotFound();
        }

        _db.Vendors.Remove(vendor);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static string FormatSpend(decimal value)
    {
        return $"NPR {value.ToString("N0", CultureInfo.InvariantCulture)}";
    }
}
