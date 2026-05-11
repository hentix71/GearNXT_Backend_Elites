namespace GearNXT_Backend.DTOs.Customer;

public class CustomerCreateRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public VehicleRequest? Vehicle { get; set; }
}

public class CustomerUpdateRequest : CustomerCreateRequest
{
}

public class VehicleRequest
{
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? VehicleNumber { get; set; }
    public string? Vin { get; set; }
}