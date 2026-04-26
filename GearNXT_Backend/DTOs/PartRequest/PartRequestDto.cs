namespace GearNXT_Backend.DTOs.PartRequest;

public class PartRequestDto
{
    public int? CustomerId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
