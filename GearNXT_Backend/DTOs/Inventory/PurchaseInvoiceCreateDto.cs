using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GearNXT_Backend.DTOs.Inventory;

public class PurchaseInvoiceCreateDto
{
    [Required]
    public int VendorId { get; set; }

    [Required]
    public DateTime InvoiceDate { get; set; }

    [MaxLength(150)]
    public string? CreatedBy { get; set; }

    [MinLength(1)]
    public List<PurchaseInvoiceItemCreateDto> Items { get; set; } = new();
}
