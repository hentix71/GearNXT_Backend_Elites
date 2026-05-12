using System.ComponentModel.DataAnnotations;

namespace GearNXT_Backend.DTOs.Auth;

public class ProfileUpdateDto
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [EmailAddress]
    [MaxLength(150)]
    public string? Email { get; set; }

    [Phone]
    [MaxLength(20)]
    public string? Phone { get; set; }
}
