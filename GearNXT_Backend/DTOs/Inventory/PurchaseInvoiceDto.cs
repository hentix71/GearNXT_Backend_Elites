using System;
using System.Collections.Generic;

namespace GearNXT_Backend.DTOs.Inventory;

public class PurchaseInvoiceDto
{
    public int Id { get; set; }
    public int VendorId { get; set; }
    public string? VendorName { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal GrandTotal => TotalAmount;
    public DateTime InvoiceDate { get; set; }
    public string? CreatedBy { get; set; }
    public string Status { get; set; } = "Active";
    public string? Notes { get; set; }
    public List<PurchaseInvoiceItemDto> Items { get; set; } = new();
}

public class PurchaseInvoiceItemDto
{
    public int PartId { get; set; }
    public string? PartName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
}
