using System;

namespace GearNXT_Backend.DTOs.Appointment;

public class AppointmentDto
{
    public int? CustomerId { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public DateTime PreferredDate { get; set; }
    public string? Notes { get; set; }
}
