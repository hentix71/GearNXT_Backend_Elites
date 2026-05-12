using System.ComponentModel.DataAnnotations;

namespace GearNXT_Backend.DTOs.Inventory;

public class PurchaseInvoiceItemCreateDto
{
    [Required]
    public int PartId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}
