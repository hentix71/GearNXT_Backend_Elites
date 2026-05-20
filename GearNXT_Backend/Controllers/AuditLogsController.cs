using GearNXT_Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GearNXT_Backend.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = "Admin")]
public class AuditLogsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuditLogsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int limit = 50,
        [FromQuery] string? sortBy = "timestamp",
        [FromQuery] string? action = null,
        [FromQuery] string? user = null)
    {
        limit = Math.Clamp(limit, 1, 500);

        var query = _db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(action))
        {
            var actionFilter = action.Trim().ToLower();
            query = query.Where(log => log.Action.ToLower().Contains(actionFilter));
        }

        if (!string.IsNullOrWhiteSpace(user))
        {
            var userFilter = user.Trim().ToLower();
            query = query.Where(log => log.UserName.ToLower().Contains(userFilter));
        }

        query = sortBy?.ToLower() switch
        {
            "action" => query.OrderByDescending(log => log.Action).ThenByDescending(log => log.Timestamp),
            "user" => query.OrderByDescending(log => log.UserName).ThenByDescending(log => log.Timestamp),
            _ => query.OrderByDescending(log => log.Timestamp),
        };

        var logs = await query
            .Take(limit)
            .Select(log => new
            {
                id = log.Id,
                action = log.Action,
                user = log.UserName,
                userId = log.UserId,
                target = log.Target,
                timestamp = log.Timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                details = log.Details ?? "",
            })
            .ToListAsync();

        return Ok(logs);
    }

    [HttpGet("purchases")]
    public async Task<IActionResult> Purchases(
        [FromQuery] int limit = 50,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? vendor = null)
    {
        limit = Math.Clamp(limit, 1, 1000);

        var q = _db.PurchaseInvoices
            .AsNoTracking()
            .Include(p => p.Vendor)
            .AsQueryable();

        if (startDate.HasValue)
            q = q.Where(p => p.InvoiceDate >= startDate.Value);
        if (endDate.HasValue)
            q = q.Where(p => p.InvoiceDate <= endDate.Value);
        if (!string.IsNullOrWhiteSpace(vendor))
        {
            var v = vendor.Trim().ToLower();
            q = q.Where(p => p.Vendor != null && p.Vendor.Name.ToLower().Contains(v));
        }

        var list = await q.OrderByDescending(p => p.InvoiceDate)
            .Take(limit)
            .Select(p => new
            {
                id = p.Id,
                vendor = p.Vendor != null ? p.Vendor.Name : "",
                totalAmount = p.TotalAmount,
                invoiceDate = p.InvoiceDate.ToString("yyyy-MM-dd"),
                createdBy = p.CreatedBy ?? "",
                status = p.Status,
                itemsCount = p.Items.Count
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("sales")]
    public async Task<IActionResult> Sales(
        [FromQuery] int limit = 50,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? staffId = null)
    {
        limit = Math.Clamp(limit, 1, 1000);

        var q = _db.SalesInvoices
            .AsNoTracking()
            .Include(s => s.Staff)
            .Include(s => s.Customer)
            .ThenInclude(c => c.User)
            .AsQueryable();

        if (startDate.HasValue)
            q = q.Where(s => s.InvoiceDate >= startDate.Value);
        if (endDate.HasValue)
            q = q.Where(s => s.InvoiceDate <= endDate.Value);
        if (staffId.HasValue)
            q = q.Where(s => s.StaffId == staffId.Value);

        var invoices = await q.OrderByDescending(s => s.InvoiceDate)
            .Take(limit)
            .Select(s => new
            {
                id = s.Id,
                invoiceNumber = s.InvoiceNumber,
                staff = s.Staff != null ? s.Staff.Name : "",
                staffId = s.StaffId,
                customer = s.Customer != null && s.Customer.User != null ? s.Customer.User.Name : "",
                grandTotal = s.GrandTotal,
                paymentStatus = s.PaymentStatus,
                paidAmount = s.PaidAmount,
                invoiceDate = s.InvoiceDate.ToString("yyyy-MM-dd")
            })
            .ToListAsync();

        // aggregate per staff
        var staffTotals = await _db.SalesInvoices
            .AsNoTracking()
            .Where(s => (!startDate.HasValue || s.InvoiceDate >= startDate.Value) && (!endDate.HasValue || s.InvoiceDate <= endDate.Value))
            .GroupBy(s => s.StaffId)
            .Select(g => new
            {
                staffId = g.Key,
                total = g.Sum(x => x.GrandTotal)
            })
            .ToListAsync();

        // enrich staff names
        var staffIds = staffTotals.Select(s => s.staffId).Where(id => id != 0).ToList();
        var staffUsers = await _db.Users
            .AsNoTracking()
            .Where(u => staffIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name);

        var staffTotalsEnriched = staffTotals.Select(s => new
        {
            staffId = s.staffId,
            staffName = staffUsers.ContainsKey(s.staffId) ? staffUsers[s.staffId] : "",
            total = s.total
        });

        var overallTotal = staffTotals.Sum(s => s.total);

        return Ok(new { invoices, staffTotals = staffTotalsEnriched, overallTotal });
    }

    [HttpGet("credits")]
    public async Task<IActionResult> Credits(
        [FromQuery] int limit = 100,
        [FromQuery] int? customerId = null,
        [FromQuery] bool onlyUnpaid = false)
    {
        limit = Math.Clamp(limit, 1, 2000);

        var q = _db.CreditPayments
            .AsNoTracking()
            .AsQueryable();

        if (customerId.HasValue)
            q = q.Where(c => c.CustomerId == customerId.Value);
        if (onlyUnpaid)
            q = q.Where(c => !c.IsPaid);

        var payments = await q.OrderByDescending(c => c.CreatedAt)
            .Take(limit)
            .Select(c => new
            {
                id = c.Id,
                customerId = c.CustomerId,
                amountDue = c.AmountDue,
                dueDate = c.DueDate.ToString("yyyy-MM-dd"),
                isPaid = c.IsPaid,
                paidAt = c.PaidAt.HasValue ? c.PaidAt.Value.ToString("yyyy-MM-dd") : "",
                createdAt = c.CreatedAt.ToString("yyyy-MM-dd")
            })
            .ToListAsync();

        // unpaid totals per customer
        var unpaidTotals = await _db.CreditPayments
            .AsNoTracking()
            .Where(c => !c.IsPaid)
            .GroupBy(c => c.CustomerId)
            .Select(g => new { customerId = g.Key, unpaidTotal = g.Sum(x => x.AmountDue) })
            .ToListAsync();

        // attach customer names if possible
        var customerIds = unpaidTotals.Select(u => u.customerId).ToList();
        var customers = await _db.Customers
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.User != null ? c.User.Name : "");

        var unpaidEnriched = unpaidTotals.Select(u => new
        {
            customerId = u.customerId,
            customerName = customers.ContainsKey(u.customerId) ? customers[u.customerId] : "",
            unpaidTotal = u.unpaidTotal
        });

        return Ok(new { payments, unpaidByCustomer = unpaidEnriched });
    }
}
