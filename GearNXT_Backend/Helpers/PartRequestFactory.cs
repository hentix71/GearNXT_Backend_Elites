using GearNXT_Backend.Data;
using GearNXT_Backend.DTOs.PartRequest;
using GearNXT_Backend.Models;

namespace GearNXT_Backend.Helpers;

public static class PartRequestFactory
{
    private static readonly string[] AllowedUrgencies = ["Routine", "Needed for repair", "Urgent"];

    public static (bool ok, string? error, PartRequest? entity) Create(
        AppDbContext db,
        int customerId,
        PartRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PartName))
        {
            return (false, "Part name is required.", null);
        }

        if (!dto.VehicleId.HasValue)
        {
            return (false, "Please select a vehicle for this part request.", null);
        }

        var vehicle = db.Vehicles.FirstOrDefault(v =>
            v.Id == dto.VehicleId.Value && v.CustomerId == customerId);
        if (vehicle == null)
        {
            return (false, "Vehicle not found on your account.", null);
        }

        var quantity = dto.Quantity < 1 ? 1 : dto.Quantity;
        var urgency = string.IsNullOrWhiteSpace(dto.Urgency) ? null : dto.Urgency.Trim();
        if (urgency != null && !AllowedUrgencies.Contains(urgency))
        {
            return (false, $"Invalid urgency. Allowed: {string.Join(", ", AllowedUrgencies)}.", null);
        }

        var partRequest = new PartRequest
        {
            CustomerId = customerId,
            VehicleId = vehicle.Id,
            PartName = dto.PartName.Trim(),
            PartNumber = string.IsNullOrWhiteSpace(dto.PartNumber) ? null : dto.PartNumber.Trim(),
            Quantity = quantity,
            Category = string.IsNullOrWhiteSpace(dto.Category) ? null : dto.Category.Trim(),
            Urgency = urgency,
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        return (true, null, partRequest);
    }
}
