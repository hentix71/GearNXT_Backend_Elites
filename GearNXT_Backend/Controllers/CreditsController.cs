using Microsoft.AspNetCore.Mvc;
using GearNXT_Backend.Data;
using GearNXT_Backend.DTOs.Credit;
using GearNXT_Backend.Models;
using GearNXT_Backend.Services;
using System.Linq;

namespace GearNXT_Backend.Controllers;

[ApiController]
[Route("api/credits")]
public class CreditsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly EmailService _email;

    public CreditsController(AppDbContext db, EmailService email)
    {
        _db = db;
        _email = email;
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreditDto dto)
    {
        var customerId = dto.CustomerId ?? GetDemoUserId();
        var c = new CreditPayment
        {
            CustomerId = customerId,
            AmountDue = dto.AmountDue,
            DueDate = dto.DueDate,
            IsPaid = false,
            CreatedAt = System.DateTime.UtcNow
        };

        _db.CreditPayments.Add(c);
        _db.SaveChanges();

        return CreatedAtAction(null, new { id = c.Id });
    }

    [HttpGet("overdue")]
    public IActionResult Overdue()
    {
        var cutoff = System.DateTime.UtcNow.AddDays(-30);
        var list = _db.CreditPayments.Where(cp => !cp.IsPaid && cp.DueDate <= cutoff).OrderBy(cp => cp.DueDate).ToList();
        return Ok(list);
    }

    [HttpPost("send-reminders")]
    public async System.Threading.Tasks.Task<IActionResult> SendReminders()
    {
        var cutoff = System.DateTime.UtcNow.AddDays(-30);
        var overdue = _db.CreditPayments.Where(cp => !cp.IsPaid && cp.DueDate <= cutoff).ToList();

        foreach (var c in overdue)
        {
            var user = _db.Users.Find(c.CustomerId);
            if (user == null) continue;
            var subject = "Overdue Payment Reminder";
            var body = $"Dear {user.Name}, your payment of {c.AmountDue:C} was due on {c.DueDate:d}. Please pay.";
            await _email.SendEmailAsync(user.Email, subject, body);
        }

        return Ok(new { count = overdue.Count });
    }

    private int GetDemoUserId()
    {
        if (Request.Headers.TryGetValue("X-User-Id", out var v) && int.TryParse(v, out var id))
            return id;
        return _db.Users.FirstOrDefault(u => u.Role == GearNXT_Backend.Models.Role.Customer)?.Id ?? 0;
    }
}
