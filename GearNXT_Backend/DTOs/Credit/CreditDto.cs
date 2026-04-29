using System;

namespace GearNXT_Backend.DTOs.Credit;

public class CreditDto
{
    public int? CustomerId { get; set; }
    public decimal AmountDue { get; set; }
    public DateTime DueDate { get; set; }
}
