using System;
using System.ComponentModel.DataAnnotations;

namespace GearNXT_Backend.Models;

public class Vehicle
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    [MaxLength(100)]
    public string? Make { get; set; }

    [MaxLength(100)]
    public string? Model { get; set; }

    public int? Year { get; set; }

    [MaxLength(50)]
    public string? LicensePlate { get; set; }

    [MaxLength(100)]
    public string? VehicleNumber { get; set; }

    [MaxLength(50)]
    public string? Color { get; set; }

    [MaxLength(50)]
    public string? FuelType { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool IsPrimary { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Customer? Customer { get; set; }
}