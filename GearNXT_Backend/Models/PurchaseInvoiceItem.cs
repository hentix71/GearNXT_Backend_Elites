using System.ComponentModel.DataAnnotations;

namespace GearNXT_Backend.Models;

public class PurchaseInvoiceItem
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }

    public int PartId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}
