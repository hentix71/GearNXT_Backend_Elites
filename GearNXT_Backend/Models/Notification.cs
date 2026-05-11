using System;
using System.ComponentModel.DataAnnotations;

namespace GearNXT_Backend.Models;

public class Notification
{
    public int Id { get; set; }

    public int? PartId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
