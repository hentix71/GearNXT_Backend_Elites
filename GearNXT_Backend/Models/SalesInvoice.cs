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

    public int StaffId { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public bool DiscountApplied { get; set; }

    public decimal GrandTotal { get; set; }

    [MaxLength(50)]
    public string PaymentStatus { get; set; } = "Paid";

    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

    public bool EmailSent { get; set; } = false;

    public Customer? Customer { get; set; }

    public ICollection<SalesInvoiceItem> Items { get; set; } = new List<SalesInvoiceItem>();

    public User? Staff { get; set; }
}
