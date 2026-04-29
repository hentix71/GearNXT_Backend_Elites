using System.Linq;
using Microsoft.AspNetCore.Mvc;
using GearNXT_Backend.Data;
using GearNXT_Backend.DTOs.Customer;
using GearNXT_Backend.Models;

namespace GearNXT_Backend.Controllers;

[ApiController]
[Route("api/customer")]
public class CustomerController : ControllerBase
{
    private readonly AppDbContext _db;

    public CustomerController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] CustomerRegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.FullName))
            return BadRequest(new { message = "Missing fields" });

        if (_db.Users.Any(u => u.Email.ToLower() == dto.Email.ToLower()))
            return Conflict(new { message = "Email already registered" });

        var user = new User
        {
            Name = dto.FullName,
            Email = dto.Email,
            PasswordHash = dto.Password ?? string.Empty,
            Role = Role.Customer,
            Phone = dto.Phone,
            IsActive = true,
            CreatedAt = System.DateTime.UtcNow
        };

        _db.Users.Add(user);
        _db.SaveChanges();

        return CreatedAtAction(null, new { id = user.Id, name = user.Name, email = user.Email });
    }

    // Profile endpoints expect an X-User-Id header for demo purposes
    private int GetDemoUserId()
    {
        if (Request.Headers.TryGetValue("X-User-Id", out var v) && int.TryParse(v, out var id))
            return id;
        return _db.Users.FirstOrDefault(u => u.Role == Role.Customer)?.Id ?? 0;
    }

    [HttpGet("profile")]
    public IActionResult Profile()
    {
        var id = GetDemoUserId();
        var user = _db.Users.Find(id);
        if (user == null) return NotFound();

        return Ok(new { id = user.Id, fullName = user.Name, email = user.Email, phone = user.Phone, role = user.Role.ToString() });
    }

    [HttpPut("profile")]
    public IActionResult UpdateProfile([FromBody] dynamic payload)
    {
        var id = GetDemoUserId();
        var user = _db.Users.Find(id);
        if (user == null) return NotFound();

        if (payload.fullName != null) user.Name = (string)payload.fullName;
        if (payload.phone != null) user.Phone = (string)payload.phone;

        user.UpdatedAt = System.DateTime.UtcNow;
        _db.SaveChanges();

        return Ok(new { ok = true });
    }
}
