using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GearNXT_Backend.Data;
using GearNXT_Backend.DTOs.Review;
using GearNXT_Backend.Models;

namespace GearNXT_Backend.Controllers;

[ApiController]
[Route("api/customer-portal/reviews")]
public class CustomerPortalReviewsController : ControllerBase
{
    private readonly AppDbContext _db;

    public CustomerPortalReviewsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public IActionResult PostReview([FromBody] ReviewDto dto)
    {
        var customerId = dto.CustomerId ?? GetDemoUserId();

        // enforce one review per month
        var latest = _db.Reviews
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();

        if (latest != null)
        {
            var nextAllowed = latest.CreatedAt.AddMonths(1);
            if (System.DateTime.UtcNow < nextAllowed)
            {
                return Conflict(new { nextAllowed = nextAllowed });
            }
        }

        var r = new Review
        {
            CustomerId = customerId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            CreatedAt = System.DateTime.UtcNow
        };

        _db.Reviews.Add(r);
        _db.SaveChanges();

        return CreatedAtAction(null, new { id = r.Id });
    }

    [HttpGet("all")]
    public IActionResult ListAll()
    {
        // Project reviews to a lightweight shape to avoid circular references during JSON serialization
        var list = _db.Reviews
            .Include(r => r.Customer)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new {
                id = r.Id,
                rating = r.Rating,
                comment = r.Comment,
                date = r.CreatedAt,
                customerId = r.CustomerId,
                customerName = r.Customer != null ? (r.Customer.User != null ? r.Customer.User.Name : null) : null
            })
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
