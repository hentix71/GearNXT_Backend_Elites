using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GearNXT_Backend.Data;
using GearNXT_Backend.DTOs.Review;
using GearNXT_Backend.Models;
using System.Linq;

namespace GearNXT_Backend.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReviewsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public IActionResult PostReview([FromBody] ReviewDto dto)
    {
        var customerId = dto.CustomerId ?? GetDemoUserId();
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

    [HttpGet]
    public IActionResult List()
    {
        var list = _db.Reviews
            .Include(r => r.Customer)
            .OrderByDescending(r => r.CreatedAt)
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
