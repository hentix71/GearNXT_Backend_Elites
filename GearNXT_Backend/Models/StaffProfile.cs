using System;
using System.ComponentModel.DataAnnotations;

namespace GearNXT_Backend.Models;

public class StaffProfile
{
    public int Id { get; set; }

    // Link to User table (canonical auth/profile storage)
    public int? UserId { get; set; }
    public User? User { get; set; }

    [MaxLength(50)]
    public string? EmployeeNumber { get; set; }
}
