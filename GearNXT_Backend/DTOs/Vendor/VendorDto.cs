using System;
using System.ComponentModel.DataAnnotations;

namespace GearNXT_Backend.DTOs.Vendor;

public class RegisterVendorDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string? VendorCode { get; set; }

    [Required]
    public string ContactPerson { get; set; } = string.Empty;

    public string? ContactTitle { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, Phone]
    public string Phone { get; set; } = string.Empty;

    public string? AlternatePhone { get; set; }

    public string? Website { get; set; }

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required]
    public string City { get; set; } = string.Empty;

    public string? District { get; set; }

    public string? Country { get; set; }

    public string? PanNumber { get; set; }

    public string? PaymentTerms { get; set; }

    public string? BankName { get; set; }

    public string? BankAccount { get; set; }

    public string? ProductCategories { get; set; }

    public string? Notes { get; set; }
}

public class ResponseVendorDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? VendorCode { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactTitle { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AlternatePhone { get; set; }
    public string? Website { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Country { get; set; }
    public string? PanNumber { get; set; }
    public string? PaymentTerms { get; set; }
    public string? BankName { get; set; }
    public string? BankAccount { get; set; }
    public string? ProductCategories { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class VendorDto
{
    public string? Name { get; set; }
    public string? VendorCode { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactTitle { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    public string? Phone { get; set; }

    public string? AlternatePhone { get; set; }
    public string? Website { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Country { get; set; }
    public string? PanNumber { get; set; }
    public string? PaymentTerms { get; set; }
    public string? BankName { get; set; }
    public string? BankAccount { get; set; }
    public string? ProductCategories { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}
