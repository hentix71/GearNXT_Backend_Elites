using System.ComponentModel.DataAnnotations;

namespace GearNXT_Backend.DTOs.Inventory;

public class AdminPartCreateDto
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? Sku { get; set; }

    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    public int? VendorId { get; set; }
}
