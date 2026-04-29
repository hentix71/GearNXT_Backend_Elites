using System;
using System.ComponentModel.DataAnnotations;

namespace GearNXT_Backend.Models;

public class CreditPayment
{
    public int Id { get; set; }

    [Required]
    public int CustomerId { get; set; }

    public decimal AmountDue { get; set; }

    public DateTime DueDate { get; set; }

    public bool IsPaid { get; set; } = false;

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
