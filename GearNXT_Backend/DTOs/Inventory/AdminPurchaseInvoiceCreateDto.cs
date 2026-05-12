using System;
using System.ComponentModel.DataAnnotations;

namespace GearNXT_Backend.DTOs.Inventory;

public class AdminPurchaseInvoiceCreateDto
{
    [Required]
    [MaxLength(150)]
    public string Vendor { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Part { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitCost { get; set; }

    public DateTime Date { get; set; }
}
