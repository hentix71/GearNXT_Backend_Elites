using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GearNXT_Backend.Models;

// Thin customer profile linked to `User`.
public class Customer
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public User? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(200)]
    public string? AddressLine { get; set; }

    [MaxLength(80)]
    public string? City { get; set; }

    [MaxLength(80)]
    public string? District { get; set; }

    [MaxLength(30)]
    public string? PreferredContactMethod { get; set; }
    
    public decimal TotalDiscountEarned { get; set; } = 0.00m;

    public decimal TotalSpent { get; set; } = 0.00m;

    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

    public ICollection<SalesInvoice> SalesInvoices { get; set; } = new List<SalesInvoice>();
    
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public ICollection<PartRequest> PartRequests { get; set; } = new List<PartRequest>();

    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}