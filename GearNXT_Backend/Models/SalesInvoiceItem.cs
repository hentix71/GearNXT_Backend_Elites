using System.ComponentModel.DataAnnotations;

namespace GearNXT_Backend.Models;

public class SalesInvoiceItem
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }

    public int PartId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    [MaxLength(120)]
    public string? PartName { get; set; }

    public SalesInvoice? Invoice { get; set; }

    public Part? Part { get; set; }
}