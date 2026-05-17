using System;

namespace GearNXT_Backend.DTOs.Inventory;

public class AdminPurchaseInvoiceDto
{
    public int Id { get; set; }
    public string Vendor { get; set; } = string.Empty;
    public string Part { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public DateTime Date { get; set; }
}
