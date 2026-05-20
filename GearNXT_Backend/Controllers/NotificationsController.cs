using System.Collections.Generic;
using System.Threading.Tasks;
using GearNXT_Backend.Models;
using GearNXT_Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace GearNXT_Backend.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly NotificationService _notificationService;

    public NotificationsController(NotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<List<Notification>>> GetUnreadNotifications([FromQuery] bool includeRead = false)
    {
        var result = await _notificationService.GetNotificationsAsync(includeRead);
        return Ok(result.Data);
    }

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var result = await _notificationService.MarkAsReadAsync(id);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Message);
        }

        return NoContent();
    }
}
