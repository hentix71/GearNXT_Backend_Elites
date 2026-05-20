using System.Linq;
using System.Security.Claims;
using GearNXT_Backend.Data;
using GearNXT_Backend.Models;
using Microsoft.AspNetCore.Http;

namespace GearNXT_Backend.Helpers;

public static class CustomerIdHelper
{
    public static int? ResolveCustomerId(HttpContext httpContext, AppDbContext db)
    {
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                var fromUser = db.Customers
                    .Where(c => c.UserId == userId)
                    .Select(c => (int?)c.Id)
                    .FirstOrDefault();
                if (fromUser.HasValue)
                {
                    return fromUser;
                }
            }
        }

        if (httpContext.Request.Headers.TryGetValue("X-Customer-Id", out var customerHeader) &&
            int.TryParse(customerHeader, out var customerId))
        {
            return customerId;
        }

        if (httpContext.Request.Headers.TryGetValue("X-User-Id", out var userHeader) &&
            int.TryParse(userHeader, out var headerId))
        {
            if (db.Customers.Any(c => c.Id == headerId))
            {
                return headerId;
            }

            var linked = db.Customers
                .Where(c => c.UserId == headerId)
                .Select(c => (int?)c.Id)
                .FirstOrDefault();
            if (linked.HasValue)
            {
                return linked;
            }
        }

        return null;
    }
}
