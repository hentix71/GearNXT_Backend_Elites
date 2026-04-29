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

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
