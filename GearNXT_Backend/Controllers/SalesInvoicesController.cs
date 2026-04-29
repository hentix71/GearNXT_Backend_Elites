using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GearNXT_Backend.Data;
using GearNXT_Backend.DTOs.Sales;
using GearNXT_Backend.Models;
using GearNXT_Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GearNXT_Backend.Controllers;

[ApiController]
[Route("api/sales-invoices")]
public class SalesInvoicesController : ControllerBase
{
    private const decimal LoyaltyThreshold = 5000m;
    private const decimal LoyaltyDiscountPercent = 10m;

    private readonly AppDbContext _db;
    private readonly EmailService _emailService;

    public SalesInvoicesController(AppDbContext db, EmailService emailService)
    {
        _db = db;
        _emailService = emailService;
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
        var staffId = request.StaffId ?? GetDemoStaffId();

        using var transaction = _db.Database.BeginTransaction();

        var invoice = new SalesInvoice
        {
            InvoiceNumber = invoiceNumber,
            CustomerId = customer.Id,
            StaffId = staffId,
            TotalAmount = totalAmount,
            DiscountAmount = discountAmount,
            DiscountApplied = discountApplied,
            GrandTotal = grandTotal,
            PaymentStatus = string.IsNullOrWhiteSpace(request.PaymentStatus) ? "Paid" : request.PaymentStatus,
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

        if (invoice.Customer == null || string.IsNullOrWhiteSpace(invoice.Customer.Email))
        {
            return BadRequest(new { message = "Customer email is missing." });
        }

        var body = BuildInvoiceEmailBody(invoice);
        await _emailService.SendEmailAsync(invoice.Customer.Email, $"Invoice {invoice.InvoiceNumber}", body);

        invoice.EmailSent = true;
        _db.SaveChanges();

        return Ok(MapInvoice(invoice));
    }

    private List<SalesInvoice> LoadInvoices()
    {
        return _db.SalesInvoices
            .Include(invoice => invoice.Customer)
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

        return new
        {
            id = invoice.Id,
            invoiceNumber = invoice.InvoiceNumber,
            customerId = invoice.CustomerId,
            customerName = invoice.Customer?.Name ?? string.Empty,
            items,
            subtotal = invoice.TotalAmount,
            discount = invoice.DiscountAmount,
            discountApplied = invoice.DiscountApplied,
            grandTotal = invoice.GrandTotal,
            paymentStatus = invoice.PaymentStatus,
            date = invoice.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            emailSent = invoice.EmailSent
        };
    }

    private string BuildInvoiceEmailBody(SalesInvoice invoice)
    {
        var lines = invoice.Items.Select(item =>
            $"- {item.PartName ?? item.Part?.Name ?? "Part"} x{item.Quantity} @ {item.UnitPrice:C} = {(item.UnitPrice * item.Quantity):C}");

        return string.Join(Environment.NewLine, new[]
        {
            $"Dear {invoice.Customer?.Name},",
            string.Empty,
            $"Thank you for your purchase. Your invoice {invoice.InvoiceNumber} is ready.",
            string.Empty,
            string.Join(Environment.NewLine, lines),
            string.Empty,
            $"Subtotal: {invoice.TotalAmount:C}",
            invoice.DiscountApplied ? $"Discount: {invoice.DiscountAmount:C}" : "Discount: 0",
            $"Grand Total: {invoice.GrandTotal:C}",
            string.Empty,
            "Regards,",
            "GearNXT Team"
        });
    }

    private string GenerateInvoiceNumber()
    {
        var nextSequence = (_db.SalesInvoices.Count() + 1).ToString("D4");
        return $"INV-{DateTime.UtcNow:yyyyMMdd}-{nextSequence}";
    }

    private int GetDemoStaffId()
    {
        if (Request.Headers.TryGetValue("X-Staff-Id", out var headerValue) && int.TryParse(headerValue, out var staffId))
        {
            return staffId;
        }

        return 1;
    }
}