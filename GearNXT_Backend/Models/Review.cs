using System;
using System.ComponentModel.DataAnnotations;

namespace GearNXT_Backend.Models;

public class Review
{
    public int Id { get; set; }

    [Required]
    public int CustomerId { get; set; }

    [Range(1,5)]
    public int Rating { get; set; }

    [MaxLength(2000)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Customer? Customer { get; set; }
}
