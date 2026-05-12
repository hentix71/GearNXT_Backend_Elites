using System;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
namespace GearNXT_Backend.Models;

[Index(nameof(Name), IsUnique = true)]
[Index(nameof(VendorCode), IsUnique = true)]
public class Vendor
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? VendorCode { get; set; }

    [MaxLength(100)]
    public string? ContactPerson { get; set; }

    [MaxLength(100)]
    public string? ContactTitle { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string? Email { get; set; }

    [Required]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(20)]
    public string? AlternatePhone { get; set; }

    [MaxLength(200)]
    public string? Website { get; set; }

    [Required]
    [MaxLength(250)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? District { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; } = "Nepal";

    [MaxLength(20)]
    public string? PanNumber { get; set; }

    [MaxLength(100)]
    public string? PaymentTerms { get; set; }

    [MaxLength(150)]
    public string? BankName { get; set; }

    [MaxLength(50)]
    public string? BankAccount { get; set; }

    [MaxLength(250)]
    public string? ProductCategories { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<Part> Parts { get; set; } = new List<Part>();

    public ICollection<PurchaseInvoice> PurchaseInvoices { get; set; } = new List<PurchaseInvoice>();
}
