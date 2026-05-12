using System.ComponentModel.DataAnnotations;

namespace GearNXT_Backend.DTOs.Inventory;

public class AdminVendorUpsertDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Contact { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Active";

    [Range(0, double.MaxValue)]
    public decimal Spend { get; set; }
}
