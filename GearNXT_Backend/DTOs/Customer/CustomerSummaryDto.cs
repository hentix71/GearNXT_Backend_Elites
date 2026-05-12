using System;
using System.Collections.Generic;

namespace GearNXT_Backend.DTOs.Customer;

public class CustomerSummaryDto
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string RegisteredDate { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public decimal TotalSpend { get; set; }
    public decimal CreditBalance { get; set; }
    public string LastVisit { get; set; } = string.Empty;
    public VehicleDto Vehicle { get; set; } = new VehicleDto();
    public List<VehicleDto> Vehicles { get; set; } = new List<VehicleDto>();
}
