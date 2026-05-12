using System;
using System.ComponentModel.DataAnnotations;

namespace GearNXT_Backend.Models;

public class Notification
{
    public int Id { get; set; }

    /// <summary>
    /// When set, only this user sees the notification. When null, Admin/Staff shared alerts (e.g. low stock).
    /// </summary>
    public int? UserId { get; set; }

    public User? User { get; set; }

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
