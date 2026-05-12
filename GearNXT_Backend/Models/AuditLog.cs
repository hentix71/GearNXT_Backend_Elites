using System.ComponentModel.DataAnnotations;

namespace GearNXT_Backend.Models;

public class AuditLog
{
    public int Id { get; set; }

    [Required]
    [MaxLength(120)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string UserName { get; set; } = "System";

    public int? UserId { get; set; }

    [MaxLength(200)]
    public string Target { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Details { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
