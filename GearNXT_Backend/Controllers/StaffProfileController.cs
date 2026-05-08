using System.Linq;
using System.Security.Claims;
using GearNXT_Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GearNXT_Backend.Controllers;

[ApiController]
[Route("api/staff/profile")]
[Authorize(Roles = "Admin,Staff")]
public class StaffProfileController : ControllerBase
{
    private readonly AppDbContext _db;

    public StaffProfileController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _db.Users.FindAsync(userId.Value);
        if (user == null)
        {
            return NotFound();
        }

        var employeeNumber = await _db.StaffProfiles
            .Where(s => s.UserId == user.Id)
            .Select(s => s.EmployeeNumber)
            .FirstOrDefaultAsync();

        return Ok(new
        {
            id = user.Id,
            fullName = user.Name,
            name = user.Name,
            email = user.Email,
            phone = user.Phone,
            role = user.Role.ToString(),
            title = user.Role.ToString(),
            department = string.Empty,
            employeeId = employeeNumber,
            employeeNumber,
            status = user.IsActive ? "Active" : "Inactive",
            createdAt = user.CreatedAt,
            lastLoginAt = user.UpdatedAt ?? user.CreatedAt
        });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] GearNXT_Backend.DTOs.Auth.ProfileUpdateDto request)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _db.Users.FindAsync(userId.Value);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            if (await _db.Users.AnyAsync(u => u.Name == name && u.Id != userId.Value))
            {
                return Conflict(new { message = "Name is already taken by another user." });
            }

            user.Name = name;
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = request.Email.Trim();
            if (await _db.Users.AnyAsync(u => u.Email == email && u.Id != userId.Value))
            {
                return Conflict(new { message = "Email is already taken by another user." });
            }

            user.Email = email;
        }

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var phone = request.Phone.Trim();
            if (await _db.Users.AnyAsync(u => u.Phone == phone && u.Id != userId.Value))
            {
                return Conflict(new { message = "Phone number is already taken by another user." });
            }

            user.Phone = phone;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await Profile();
    }

    [HttpGet("sales-history")]
    public async Task<IActionResult> SalesHistory()
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var invoices = await _db.SalesInvoices
            .Include(i => i.Customer)
                .ThenInclude(c => c!.User)
            .Where(i => i.StaffId == userId.Value)
            .OrderByDescending(i => i.InvoiceDate)
            .Take(20)
            .Select(i => new
            {
                id = i.Id,
                invoiceNumber = i.InvoiceNumber,
                date = i.InvoiceDate.ToString("yyyy-MM-dd"),
                customerName = i.Customer != null ? i.Customer.User!.Name : "",
                grandTotal = i.GrandTotal,
                total = i.GrandTotal,
                paymentStatus = i.PaymentStatus,
                items = i.Items
            })
            .ToListAsync();

        return Ok(invoices);
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
