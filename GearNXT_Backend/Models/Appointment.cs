using System;
using System.ComponentModel.DataAnnotations;

namespace GearNXT_Backend.Models;

public class Appointment
{
    public int Id { get; set; }

    [Required]
    public int CustomerId { get; set; }

    [Required]
    [MaxLength(200)]
    public string ServiceType { get; set; } = string.Empty;

    public DateTime PreferredDate { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Upcoming";

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
