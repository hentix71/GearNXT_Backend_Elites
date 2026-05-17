using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GearNXT_Backend.Data;
using GearNXT_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace GearNXT_Backend.Services;

public class LowStockNotifier
{
    private readonly AppDbContext _db;

    public LowStockNotifier(AppDbContext db)
    {
        _db = db;
    }

    public async Task CreateLowStockNotificationsAsync()
    {
        var lowStockParts = await _db.Parts
            .AsNoTracking()
            .Include(p => p.Vendor)
            .Where(p => p.StockQuantity < 10)
            .ToListAsync();

        if (lowStockParts.Count == 0)
        {
            return;
        }

        await CreateLowStockNotificationsAsync(lowStockParts.Select(p => p.Id));
    }

    public async Task CreateLowStockNotificationsAsync(IEnumerable<int> partIds)
    {
        var lowStockParts = await _db.Parts
            .AsNoTracking()
            .Include(p => p.Vendor)
            .Where(p => partIds.Contains(p.Id) && p.StockQuantity < 10)
            .ToListAsync();

        if (lowStockParts.Count == 0)
        {
            return;
        }

        foreach (var part in lowStockParts)
        {
            var message = $"Part {part.Name} stock is low ({part.StockQuantity}).";
            var exists = await _db.Notifications.AnyAsync(n =>
                !n.IsRead && n.Type == "LowStock" && n.PartId == part.Id);

            if (!exists)
            {
                _db.Notifications.Add(new Notification
                {
                    PartId = part.Id,
                    Type = "LowStock",
                    Message = message,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync();
    }
}
