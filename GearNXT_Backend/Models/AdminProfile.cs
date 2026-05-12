using System;
using System.ComponentModel.DataAnnotations;

namespace GearNXT_Backend.Models;

public class AdminProfile
{
    public int Id { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }
    // Intentionally thin profile: no duplicated user fields here.
}
