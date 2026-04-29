using Microsoft.AspNetCore.Mvc;
using GearNXT_Backend.Data;
using GearNXT_Backend.DTOs.PartRequest;
using GearNXT_Backend.Models;
using System.Linq;

namespace GearNXT_Backend.Controllers;

[ApiController]
[Route("api/part-requests")]
public class PartRequestsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PartRequestsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public IActionResult RequestPart([FromBody] PartRequestDto dto)
    {
        var customerId = dto.CustomerId ?? GetDemoUserId();
        var pr = new PartRequest
        {
            CustomerId = customerId,
            PartName = dto.PartName,
            Description = dto.Description,
            Status = "Pending",
            CreatedAt = System.DateTime.UtcNow
        };

        _db.PartRequests.Add(pr);
        _db.SaveChanges();

        return CreatedAtAction(null, new { id = pr.Id });
    }

    private int GetDemoUserId()
    {
        if (Request.Headers.TryGetValue("X-User-Id", out var v) && int.TryParse(v, out var id))
            return id;
        return _db.Users.FirstOrDefault(u => u.Role == GearNXT_Backend.Models.Role.Customer)?.Id ?? 0;
    }
}
