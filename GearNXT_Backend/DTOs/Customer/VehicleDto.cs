using System;

namespace GearNXT_Backend.DTOs.Customer;

public class VehicleDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string Vin { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public string VehicleNumber { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
