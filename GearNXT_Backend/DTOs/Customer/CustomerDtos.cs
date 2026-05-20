using System.Text.Json.Serialization;

namespace GearNXT_Backend.DTOs.Customer;

public class CustomerCreateRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    // Optional password fields: when set by staff/admin during registration,
    // the backend will use this password for the created user instead of
    // generating a random one.
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("addressLine")]
    public string? AddressLine { get; set; }

    public string? City { get; set; }
    public string? District { get; set; }
    public string? PreferredContactMethod { get; set; }
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
    public string? Color { get; set; }
    public string? FuelType { get; set; }
    public string? Notes { get; set; }
    public bool? IsPrimary { get; set; }
}