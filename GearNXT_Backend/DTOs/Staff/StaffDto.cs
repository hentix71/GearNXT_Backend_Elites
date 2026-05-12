using System;
using System.ComponentModel.DataAnnotations;
namespace GearNXT_Backend.DTOs.Staff;

public class StaffDto
{
    public string? Name { get; set; } 
    [EmailAddress]
    public string? Email { get; set; }
    [Phone]
    public string? Phone { get; set; }
    public string? EmployeeNumber { get; set; }
}
