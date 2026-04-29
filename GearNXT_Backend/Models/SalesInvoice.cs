using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GearNXT_Backend.Models;

public class SalesInvoice
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Discount { get; set; }

    public bool DiscountApplied { get; set; }

    public decimal GrandTotal { get; set; }

    [MaxLength(50)]
    public string PaymentStatus { get; set; } = "Paid";

    public DateTime Date { get; set; } = DateTime.UtcNow;

    public bool EmailSent { get; set; } = false;
}
