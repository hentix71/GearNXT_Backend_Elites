using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GearNXT_Backend.Data;

namespace GearNXT_Backend.Controllers;

[ApiController]
[Route("api/customer-portal")]
public class CustomerPortalController : ControllerBase
{
    private readonly AppDbContext _db;

    public CustomerPortalController(AppDbContext db)
    {
        _db = db;
    }

    private int? GetCustomerIdFromHeader()
    {
        if (Request.Headers.TryGetValue("X-Customer-Id", out var v) && int.TryParse(v, out var id))
            return id;
        if (Request.Headers.TryGetValue("X-User-Id", out var u) && int.TryParse(u, out var uid))
            return uid;
        return null;
    }

    [HttpGet("profile")]
    public IActionResult Profile()
    {
        var customerId = GetCustomerIdFromHeader();
        if (customerId == null) return BadRequest(new { message = "X-Customer-Id or X-User-Id header required" });
        var id = customerId.Value;

        // Get the linked user id (if any) but avoid selecting customer columns that may not exist in some DB schemas
        var userId = _db.Customers.Where(c => c.Id == id).Select(c => c.UserId).FirstOrDefault();
        var user = userId != null ? _db.Users.FirstOrDefault(u => u.Id == userId.Value) : null;

        var vehicleCount = _db.Vehicles.Count(v => v.CustomerId == id);

        return Ok(new {
            id,
            fullName = user?.Name ?? string.Empty,
            email = user?.Email ?? string.Empty,
            phone = user?.Phone ?? string.Empty,
            role = "Customer",
            vehicleCount,
        });
    }

    [HttpGet("vehicles")]
    public IActionResult Vehicles()
    {
        var customerId = GetCustomerIdFromHeader();
        if (customerId == null) return BadRequest(new { message = "X-Customer-Id or X-User-Id header required" });

        var vehicles = _db.Vehicles
            .Where(v => v.CustomerId == customerId.Value)
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new {
                id = v.Id,
                customerId = v.CustomerId,
                model = v.Model,
                make = v.Make,
                licensePlate = v.LicensePlate,
                vehicleNumber = v.VehicleNumber,
                year = v.Year,
                createdAt = v.CreatedAt
            })
            .ToList();

        return Ok(vehicles);
    }

    [HttpGet("purchases")]
    public IActionResult Purchases()
    {
        var customerId = GetCustomerIdFromHeader();
        if (customerId == null) return BadRequest(new { message = "X-Customer-Id or X-User-Id header required" });

        var invoices = _db.SalesInvoices
            .Include(i => i.Items)
                .ThenInclude(it => it.Part)
            .Where(i => i.CustomerId == customerId.Value)
            .OrderByDescending(i => i.InvoiceDate)
            .Select(i => new {
                id = i.Id,
                invoiceNumber = i.InvoiceNumber,
                customerId = i.CustomerId,
                items = i.Items.Select(it => new { partName = it.PartName ?? (it.Part != null ? it.Part.Name : string.Empty), quantity = it.Quantity, unitPrice = it.UnitPrice }).ToList(),
                subtotal = i.TotalAmount,
                discount = i.DiscountAmount,
                grandTotal = i.GrandTotal,
                paymentStatus = i.PaymentStatus,
                date = i.InvoiceDate
            })
            .ToList();

        return Ok(invoices);
    }

    [HttpGet("reviews")]
    public IActionResult Reviews()
    {
        var customerId = GetCustomerIdFromHeader();
        if (customerId == null) return BadRequest(new { message = "X-Customer-Id or X-User-Id header required" });

        var reviews = _db.Reviews
            .Where(r => r.CustomerId == customerId.Value)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new {
                id = r.Id,
                customerId = r.CustomerId,
                rating = r.Rating,
                comment = r.Comment,
                date = r.CreatedAt
            })
            .ToList();

        return Ok(reviews);
    }
}
