using System.Globalization;
using GearNXT_Backend.Data;
using GearNXT_Backend.DTOs.Credit;
using GearNXT_Backend.DTOs.Sales;
using GearNXT_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GearNXT_Backend.Controllers;

[ApiController]
[Route("api/credits")]
[Authorize(Roles = "Admin,Staff")]
public class CreditsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly EmailService _email;

    public CreditsController(AppDbContext db, EmailService email)
    {
        _db = db;
        _email = email;
    }

    [HttpGet("outstanding")]
    public async Task<IActionResult> Outstanding()
    {
        var invoices = await LoadCreditInvoicesAsync();
        return Ok(invoices.Select(MapCreditInvoice));
    }

    [HttpGet("overdue")]
    public async Task<IActionResult> Overdue()
    {
        var today = DateTime.UtcNow.Date;
        var invoices = await LoadCreditInvoicesAsync();
        var overdue = invoices
            .Where(invoice => GetDueDate(invoice.InvoiceDate).Date < today)
            .OrderBy(invoice => invoice.InvoiceDate);

        return Ok(overdue.Select(MapCreditInvoice));
    }

    [HttpPost("send-reminders")]
    public async Task<IActionResult> SendReminders([FromBody] CreditReminderRequest? request = null)
    {
        var today = DateTime.UtcNow.Date;
        var query = await LoadCreditInvoicesAsync();

        if (request?.InvoiceId is int invoiceId)
        {
            query = query.Where(invoice => invoice.Id == invoiceId).ToList();
        }
        else
        {
            query = query
                .Where(invoice => GetDueDate(invoice.InvoiceDate).Date < today)
                .ToList();
        }

        var sent = 0;
        foreach (var invoice in query)
        {
            var email = invoice.Customer?.User?.Email;
            if (string.IsNullOrWhiteSpace(email) || email.EndsWith("@gearnxt.local", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var remaining = invoice.GrandTotal - invoice.PaidAmount;
            var subject = "Overdue Payment Reminder";
            var body =
                $"Dear {invoice.Customer?.User?.Name},\n\n" +
                $"Your invoice {invoice.InvoiceNumber} has an outstanding balance of {remaining:C}. " +
                $"The payment was due on {GetDueDate(invoice.InvoiceDate):d}. Please settle at your earliest convenience.\n\n" +
                "Regards,\nGearNXT Team";

            await _email.SendEmailAsync(email, subject, body);
            sent++;
        }

        return Ok(new { count = sent, message = sent > 0 ? "Reminder(s) sent." : "No reminders sent (missing or local email)." });
    }

    [HttpPost("{id:int}/record-payment")]
    public async Task<IActionResult> RecordPayment(int id, [FromBody] SalesInvoicePaymentRequest request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest(new { message = "Payment amount must be greater than zero." });
        }

        var invoice = await _db.SalesInvoices
            .Include(item => item.Customer)
                .ThenInclude(customer => customer!.User)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (invoice == null)
        {
            return NotFound(new { message = "Sales invoice not found." });
        }

        if (!string.Equals(invoice.PaymentStatus, "Credit", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Only credit invoices can receive payments." });
        }

        var remaining = Math.Max(0m, invoice.GrandTotal - invoice.PaidAmount);
        if (remaining <= 0)
        {
            return BadRequest(new { message = "This invoice has no remaining balance." });
        }

        if (request.Amount > remaining)
        {
            return BadRequest(new { message = $"Payment cannot exceed remaining balance of {remaining:C}." });
        }

        invoice.PaidAmount += request.Amount;
        var updatedRemaining = Math.Max(0m, invoice.GrandTotal - invoice.PaidAmount);

        if (updatedRemaining <= 0)
        {
            invoice.PaidAmount = invoice.GrandTotal;
            invoice.PaymentStatus = "Paid";
        }

        await _db.SaveChangesAsync();

        return Ok(MapCreditInvoice(invoice));
    }

    private async Task<List<Models.SalesInvoice>> LoadCreditInvoicesAsync()
    {
        return await _db.SalesInvoices
            .Include(invoice => invoice.Customer)
                .ThenInclude(customer => customer!.User)
            .Where(invoice => invoice.PaymentStatus == "Credit")
            .OrderBy(invoice => invoice.InvoiceDate)
            .ToListAsync();
    }

    private static DateTime GetDueDate(DateTime invoiceDate)
    {
        return invoiceDate.AddDays(30);
    }

    private static object MapCreditInvoice(Models.SalesInvoice invoice)
    {
        var remaining = Math.Max(0m, invoice.GrandTotal - invoice.PaidAmount);
        var dueDate = GetDueDate(invoice.InvoiceDate);

        return new
        {
            id = invoice.Id,
            invoiceNumber = invoice.InvoiceNumber,
            customerId = invoice.CustomerId,
            customerName = invoice.Customer?.User?.Name ?? "",
            subtotal = invoice.TotalAmount,
            grandTotal = invoice.GrandTotal,
            paidAmount = invoice.PaidAmount,
            remainingAmount = remaining,
            paymentStatus = invoice.PaymentStatus,
            date = invoice.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            dueDate = dueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            isOverdue = dueDate.Date < DateTime.UtcNow.Date
        };
    }
}
