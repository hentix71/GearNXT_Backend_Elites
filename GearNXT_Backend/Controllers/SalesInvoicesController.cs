using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GearNXT_Backend.Data;
using GearNXT_Backend.DTOs.Sales;
using GearNXT_Backend.Models;
using GearNXT_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GearNXT_Backend.Controllers;

[ApiController]
[Route("api/sales-invoices")]
[Authorize(Roles = "Admin,Staff")]
public class SalesInvoicesController : ControllerBase
{
    private const decimal LoyaltyThreshold = 5000m;
    private const decimal LoyaltyDiscountPercent = 10m;

    private readonly AppDbContext _db;
    private readonly EmailService _emailService;
    private readonly NotificationService _notificationService;
    private readonly ILogger<SalesInvoicesController> _logger;

    public SalesInvoicesController(AppDbContext db, EmailService emailService, NotificationService notificationService, ILogger<SalesInvoicesController> logger)
    {
        _db = db;
        _emailService = emailService;
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpGet]
    [HttpGet("/api/sales/invoices")]
    public IActionResult GetAll()
    {
        var invoices = LoadInvoices()
            .OrderByDescending(invoice => invoice.InvoiceDate)
            .Select(MapInvoice)
            .ToList();

        return Ok(invoices);
    }

    [HttpGet("{id:int}/pdf")]
    [HttpGet("/api/sales/invoices/{id:int}/pdf")]
    public IActionResult DownloadPdf(int id)
    {
        var invoice = LoadInvoices().FirstOrDefault(item => item.Id == id);
        if (invoice == null)
        {
            return NotFound(new { message = "Sales invoice not found." });
        }

        var pdfBytes = SalesInvoicePdfBuilder.Build(invoice);
        return File(pdfBytes, "application/pdf", $"invoice_{invoice.InvoiceNumber}.pdf");
    }

    [HttpGet("{id:int}")]
    [HttpGet("/api/sales/invoices/{id:int}")]
    public IActionResult GetById(int id)
    {
        var invoice = LoadInvoices().FirstOrDefault(item => item.Id == id);
        if (invoice == null)
        {
            return NotFound(new { message = "Sales invoice not found." });
        }

        return Ok(MapInvoice(invoice));
    }

    [HttpPost]
    [HttpPost("/api/sales/invoices")]
    public IActionResult Create([FromBody] SalesInvoiceCreateRequest request)
    {
        var customer = _db.Customers.Include(item => item.Vehicles).FirstOrDefault(item => item.Id == request.CustomerId);
        if (customer == null)
        {
            return NotFound(new { message = "Customer not found." });
        }

        if (request.Items.Count == 0)
        {
            return BadRequest(new { message = "At least one invoice item is required." });
        }

        var duplicatePartIds = request.Items
            .Where(item => item.PartId.HasValue)
            .GroupBy(item => item.PartId!.Value)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicatePartIds.Count > 0)
        {
            return BadRequest(new { message = "Duplicate parts are not allowed on the same invoice." });
        }

        var resolvedItems = new List<(Part Part, int Quantity, decimal UnitPrice)>();

        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
            {
                return BadRequest(new { message = "Item quantity must be greater than zero." });
            }

            Part? part = null;

            if (item.PartId.HasValue)
            {
                part = _db.Parts.FirstOrDefault(entry => entry.Id == item.PartId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(item.PartName))
            {
                part = _db.Parts.FirstOrDefault(entry => entry.Name.ToLower() == item.PartName.ToLower());
            }

            if (part == null)
            {
                return BadRequest(new { message = "One or more selected parts do not exist." });
            }

            if (!part.IsActive)
            {
                return BadRequest(new { message = $"Part '{part.Name}' is inactive and cannot be sold." });
            }

            if (part.StockQuantity < item.Quantity)
            {
                return BadRequest(new { message = $"Insufficient stock for {part.Name}." });
            }

            var unitPrice = item.UnitPrice.HasValue && item.UnitPrice.Value > 0 ? item.UnitPrice.Value : part.Price;
            resolvedItems.Add((part, item.Quantity, unitPrice));
        }

        var totalAmount = resolvedItems.Sum(item => item.UnitPrice * item.Quantity);
        var discountApplied = totalAmount > LoyaltyThreshold;
        var discountAmount = discountApplied ? Math.Round(totalAmount * (LoyaltyDiscountPercent / 100m), 2) : 0m;
        var grandTotal = totalAmount - discountAmount;
        var invoiceNumber = GenerateInvoiceNumber();
        var staffId = request.StaffId ?? GetCallerUserId();
        if (staffId == null)
        {
            return Unauthorized(new { message = "Authenticated staff identity is required." });
        }

        using var transaction = _db.Database.BeginTransaction();

        var paymentStatus = string.IsNullOrWhiteSpace(request.PaymentStatus) ? "Paid" : request.PaymentStatus;
        var isCredit = string.Equals(paymentStatus, "Credit", StringComparison.OrdinalIgnoreCase);

        var invoice = new SalesInvoice
        {
            InvoiceNumber = invoiceNumber,
            CustomerId = customer.Id,
            StaffId = staffId.Value,
            TotalAmount = totalAmount,
            DiscountAmount = discountAmount,
            DiscountApplied = discountApplied,
            GrandTotal = grandTotal,
            PaymentStatus = paymentStatus,
            PaidAmount = isCredit ? 0m : grandTotal,
            InvoiceDate = DateTime.UtcNow,
            EmailSent = false
        };

        _db.SalesInvoices.Add(invoice);
        _db.SaveChanges();

        foreach (var item in resolvedItems)
        {
            item.Part.StockQuantity -= item.Quantity;

            _db.SalesInvoiceItems.Add(new SalesInvoiceItem
            {
                InvoiceId = invoice.Id,
                PartId = item.Part.Id,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                PartName = item.Part.Name
            });
        }

        _db.SaveChanges();
        transaction.Commit();

        if (customer.UserId.HasValue)
        {
            _notificationService.NotifyUserAsync(
                customer.UserId.Value,
                "Invoice",
                $"Your invoice {invoiceNumber} has been created for NPR {grandTotal:N0}.").GetAwaiter().GetResult();
        }

        var created = LoadInvoices().First(item => item.Id == invoice.Id);
        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, MapInvoice(created));
    }

    [HttpPost("{id:int}/send-email")]
    [HttpPost("/api/sales/invoices/{id:int}/email")]
    public async Task<IActionResult> SendEmail(int id)
    {
        var invoice = LoadInvoices().FirstOrDefault(item => item.Id == id);
        if (invoice == null)
        {
            return NotFound(new { message = "Sales invoice not found." });
        }

        var customerEmail = invoice.Customer?.User?.Email;
        if (string.IsNullOrWhiteSpace(customerEmail))
        {
            _logger.LogWarning("Invoice email blocked because customer email is missing. InvoiceId={InvoiceId}", id);
            return BadRequest(new { message = "Customer email is missing." });
        }

        try
        {
            var body = BuildInvoiceEmailBody(invoice);
            var pdfBytes = SalesInvoicePdfBuilder.Build(invoice);
            await _emailService.SendEmailWithAttachmentAsync(
                customerEmail,
                $"Invoice {invoice.InvoiceNumber}",
                body,
                pdfBytes,
                $"invoice_{invoice.InvoiceNumber}.pdf");

            invoice.EmailSent = true;
            _db.SaveChanges();

            _logger.LogInformation("Invoice email sent successfully. InvoiceId={InvoiceId}, CustomerEmail={CustomerEmail}", id, customerEmail);
            return Ok(MapInvoice(invoice));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send invoice email. InvoiceId={InvoiceId}, CustomerEmail={CustomerEmail}", id, customerEmail);
            return StatusCode(500, new { message = "Unable to send invoice email. Please try again later." });
        }
    }

    [HttpPost("{id:int}/record-payment")]
    [HttpPost("/api/sales/invoices/{id:int}/record-payment")]
    public IActionResult RecordPayment(int id, [FromBody] SalesInvoicePaymentRequest request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest(new { message = "Payment amount must be greater than zero." });
        }

        var invoice = LoadInvoices().FirstOrDefault(item => item.Id == id);
        if (invoice == null)
        {
            return NotFound(new { message = "Sales invoice not found." });
        }

        if (!string.Equals(invoice.PaymentStatus, "Credit", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Only credit invoices can receive payments." });
        }

        var remaining = invoice.GrandTotal - invoice.PaidAmount;
        if (remaining <= 0)
        {
            return BadRequest(new { message = "This invoice has no remaining balance." });
        }

        if (request.Amount > remaining)
        {
            return BadRequest(new { message = $"Payment cannot exceed remaining balance of {remaining:C}." });
        }

        invoice.PaidAmount += request.Amount;
        var newRemaining = invoice.GrandTotal - invoice.PaidAmount;

        if (newRemaining <= 0)
        {
            invoice.PaidAmount = invoice.GrandTotal;
            invoice.PaymentStatus = "Paid";
        }

        _db.SaveChanges();

        return Ok(MapInvoice(invoice));
    }

    private List<SalesInvoice> LoadInvoices()
    {
        return _db.SalesInvoices
            .Include(invoice => invoice.Customer)
            .ThenInclude(customer => customer.User)
            .Include(invoice => invoice.Staff)
            .Include(invoice => invoice.Items)
            .ThenInclude(item => item.Part)
            .ToList();
    }

    private object MapInvoice(SalesInvoice invoice)
    {
        var items = invoice.Items
            .OrderBy(item => item.Id)
            .Select(item => new
            {
                id = item.Id,
                partId = item.PartId,
                partName = item.PartName ?? item.Part?.Name ?? string.Empty,
                quantity = item.Quantity,
                unitPrice = item.UnitPrice,
                total = item.UnitPrice * item.Quantity
            })
            .ToList();

        var remainingAmount = Math.Max(0m, invoice.GrandTotal - invoice.PaidAmount);

        return new
        {
            id = invoice.Id,
            invoiceNumber = invoice.InvoiceNumber,
            customerId = invoice.CustomerId,
            customerName = invoice.Customer?.User?.Name ?? string.Empty,
            items,
            subtotal = invoice.TotalAmount,
            discount = invoice.DiscountAmount,
            discountApplied = invoice.DiscountApplied,
            grandTotal = invoice.GrandTotal,
            paymentStatus = invoice.PaymentStatus,
            paidAmount = invoice.PaidAmount,
            remainingAmount,
            date = invoice.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            emailSent = invoice.EmailSent,
            staffId = invoice.StaffId,
            staffName = invoice.Staff?.Name ?? string.Empty
        };
    }

    private string BuildInvoiceEmailBody(SalesInvoice invoice)
    {
        var customerName = invoice.Customer?.User?.Name ?? "Customer";
        var itemLines = invoice.Items.Select(item =>
            $"- {item.PartName ?? item.Part?.Name ?? "Part"} x{item.Quantity} @ {item.UnitPrice:C} = {(item.UnitPrice * item.Quantity):C}");

        var remainingAmount = Math.Max(0m, invoice.GrandTotal - invoice.PaidAmount);
        var invoiceDate = invoice.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return string.Join(Environment.NewLine, new[]
        {
            $"Dear {customerName},",
            string.Empty,
            "Your sales invoice has been generated. Please see the details below:",
            string.Empty,
            $"Invoice Number: {invoice.InvoiceNumber}",
            $"Invoice Date: {invoiceDate}",
            $"Customer: {customerName}",
            string.Empty,
            "Invoice Items:",
            string.Join(Environment.NewLine, itemLines),
            string.Empty,
            $"Subtotal: {invoice.TotalAmount:C}",
            invoice.DiscountApplied ? $"Discount: {invoice.DiscountAmount:C}" : "Discount: 0",
            $"Grand Total: {invoice.GrandTotal:C}",
            $"Paid Amount: {invoice.PaidAmount:C}",
            $"Remaining Amount: {remainingAmount:C}",
            string.Empty,
            "Please contact us if you have any questions.",
            string.Empty,
            "Regards,",
            "GearNXT"
        });
    }

    private string GenerateInvoiceNumber()
    {
        var nextSequence = (_db.SalesInvoices.Count() + 1).ToString("D4");
        return $"INV-{DateTime.UtcNow:yyyyMMdd}-{nextSequence}";
    }

    private int? GetCallerUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idClaim, out var id))
        {
            return id;
        }

        return null;
    }

}