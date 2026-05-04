using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GearNXT_Backend.Data;
using GearNXT_Backend.Services;
using Microsoft.AspNetCore.Authorization;

namespace GearNXT_Backend.Controllers;

[ApiController]
[Route("api/admin/invoices")]
[Authorize(Roles = "Admin,Staff")]
public class InvoicesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly EmailService _emailService;

    public InvoicesController(AppDbContext db, EmailService emailService)
    {
        _db = db;
        _emailService = emailService;
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> DownloadInvoicePdf(int id)
    {
        var invoice = await LoadInvoiceAsync(id);
        if (invoice == null)
        {
            return NotFound();
        }

        var pdfBytes = SalesInvoicePdfBuilder.Build(invoice);
        var fileName = $"invoice_{invoice.InvoiceNumber}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    [HttpPost("{id}/send-email")]
    public async Task<IActionResult> SendInvoiceEmail(int id)
    {
        var invoice = await LoadInvoiceAsync(id);
        if (invoice == null)
        {
            return NotFound();
        }

        var user = invoice.Customer?.User;
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
        {
            return BadRequest(new { message = "Invoice has no customer email" });
        }

        var pdfBytes = SalesInvoicePdfBuilder.Build(invoice);
        var subject = $"Invoice #{invoice.InvoiceNumber}";
        var body = $"Dear {user.Name},\n\nPlease find your invoice attached.\n\nRegards,\nGearNXT";

        await _emailService.SendEmailWithAttachmentAsync(
            user.Email,
            subject,
            body,
            pdfBytes,
            $"invoice_{invoice.InvoiceNumber}.pdf");

        invoice.EmailSent = true;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Email sent" });
    }

    private async Task<Models.SalesInvoice?> LoadInvoiceAsync(int id)
    {
        return await _db.SalesInvoices
            .Include(i => i.Customer)
                .ThenInclude(c => c!.User)
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id);
    }
}
