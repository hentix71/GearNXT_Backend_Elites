using System;
using System.ComponentModel.DataAnnotations;

namespace GearNXT_Backend.DTOs.Vendor;

public class RegisterVendorDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string? Email { get; set; }
    
    [Required, Phone]
    public string? Phone { get; set; }
    
    [Required]
    public string? Address { get; set; }
}

public class ResponseVendorDto
{

    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class VendorDto
{
    public string? Name { get; set; }

    [EmailAddress]
    public string? Email { get; set; }
    
    [Phone]
    public string? Phone { get; set; }
    
    public string? Address { get; set; }
    
    public bool IsActive { get; set; }
}
