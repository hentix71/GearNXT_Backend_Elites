using System;
using System.Collections.Generic;

namespace GearNXT_Backend.DTOs.Inventory;

public class PurchaseInvoiceDto
{
    public int Id { get; set; }
    public int VendorId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime InvoiceDate { get; set; }
    public string? CreatedBy { get; set; }
    public List<PurchaseInvoiceItemDto> Items { get; set; } = new();
}

public class PurchaseInvoiceItemDto
{
    public int PartId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
