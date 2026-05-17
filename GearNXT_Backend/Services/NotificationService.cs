using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GearNXT_Backend.Data;
using GearNXT_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace GearNXT_Backend.Services;

public class NotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<List<Notification>>> GetNotificationsAsync(bool includeRead)
    {
        var query = _db.Notifications.AsNoTracking().AsQueryable();
        if (!includeRead)
        {
            query = query.Where(n => !n.IsRead);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return ServiceResult<List<Notification>>.Ok(notifications);
    }

    public async Task<ServiceResult<object>> MarkAsReadAsync(int id)
    {
        var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id);
        if (notification == null)
        {
            return ServiceResult<object>.Fail(404, "Notification not found.");
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _db.SaveChangesAsync();
        }

        return ServiceResult<object>.Ok(new { ok = true }, 204);
    }
}
