using System.Collections.Generic;

namespace GearNXT_Backend.DTOs.Sales;

public class SalesInvoiceCreateRequest
{
    public int CustomerId { get; set; }
    public int? StaffId { get; set; }
    public string? PaymentStatus { get; set; }
    public List<SalesInvoiceLineRequest> Items { get; set; } = new();
}

public class SalesInvoiceLineRequest
{
    public int? PartId { get; set; }
    public string? PartName { get; set; }
    public int Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
}