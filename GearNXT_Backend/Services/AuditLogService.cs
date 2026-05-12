using GearNXT_Backend.Data;
using GearNXT_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace GearNXT_Backend.Services;

public class AuditLogService
{
    private readonly AppDbContext _db;

    public AuditLogService(AppDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(
        string action,
        string userName,
        string target,
        string? details = null,
        int? userId = null,
        CancellationToken cancellationToken = default)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Action = action.Trim(),
            UserName = string.IsNullOrWhiteSpace(userName) ? "System" : userName.Trim(),
            UserId = userId,
            Target = target.Trim(),
            Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim(),
            Timestamp = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
