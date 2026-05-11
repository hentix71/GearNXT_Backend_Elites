namespace GearNXT_Backend.DTOs.Inventory;

public class AdminVendorDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public string Spend { get; set; } = "NPR 0";
}
