using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GearNXT_Backend.Data;
using GearNXT_Backend.DTOs.Appointment;
using GearNXT_Backend.Models;

namespace GearNXT_Backend.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AppointmentsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public IActionResult Book([FromBody] AppointmentDto dto)
    {
        var customerId = dto.CustomerId ?? GetDemoUserId();
        var appt = new Appointment
        {
            CustomerId = customerId,
            ServiceType = dto.ServiceType,
            PreferredDate = dto.PreferredDate,
            Notes = dto.Notes,
            Status = "Upcoming",
            CreatedAt = System.DateTime.UtcNow
        };

        _db.Appointments.Add(appt);
        _db.SaveChanges();

        return CreatedAtAction(null, new { id = appt.Id });
    }

    [HttpGet("my")]
    public IActionResult MyAppointments()
    {
        var id = GetDemoUserId();
        var list = _db.Appointments
            .Include(a => a.Customer)
            .Where(a => a.CustomerId == id)
            .OrderByDescending(a => a.PreferredDate)
            .ToList();
        return Ok(list);
    }

    private int GetDemoUserId()
    {
        if (Request.Headers.TryGetValue("X-User-Id", out var v) && int.TryParse(v, out var id))
            return id;
        return _db.Users.FirstOrDefault(u => u.Role == GearNXT_Backend.Models.Role.Customer)?.Id ?? 0;
    }
}
