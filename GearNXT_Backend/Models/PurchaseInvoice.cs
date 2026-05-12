using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GearNXT_Backend.Models;

public class PurchaseInvoice
{
    public int Id { get; set; }

    public int VendorId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    public DateTime InvoiceDate { get; set; }

    [MaxLength(150)]
    public string? CreatedBy { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Active";

    public Vendor? Vendor { get; set; }

    public ICollection<PurchaseInvoiceItem> Items { get; set; } = new List<PurchaseInvoiceItem>();
}
