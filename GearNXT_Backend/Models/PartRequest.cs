using System;
using System.ComponentModel.DataAnnotations;

namespace GearNXT_Backend.Models;

public class PartRequest
{
    public int Id { get; set; }

    [Required]
    public int CustomerId { get; set; }

    [Required]
    [MaxLength(200)]
    public string PartName { get; set; } = string.Empty;

    public int? VehicleId { get; set; }

    [MaxLength(80)]
    public string? PartNumber { get; set; }

    public int Quantity { get; set; } = 1;

    [MaxLength(50)]
    public string? Category { get; set; }

    [MaxLength(30)]
    public string? Urgency { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    [MaxLength(2000)]
    public string? StaffComment { get; set; }

    public string? StatusHistoryJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Customer? Customer { get; set; }

    public Vehicle? Vehicle { get; set; }
}
