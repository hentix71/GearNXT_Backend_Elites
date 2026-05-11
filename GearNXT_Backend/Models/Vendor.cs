using System;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
namespace GearNXT_Backend.Models;

[Index(nameof(Name), IsUnique = true)]
public class Vendor
{
public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string? Email { get; set; }

        [Required]
        [MaxLength(20)]
        public string? Phone { get; set; }

        [Required]
        [MaxLength(250)]
        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
}
