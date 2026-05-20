using System.Text.Json;
using GearNXT_Backend.Models;

namespace GearNXT_Backend.Helpers;

public static class PartRequestMapper
{
    public static string BuildVehicleInfo(PartRequest partRequest, Customer? customer)
    {
        if (partRequest.Vehicle != null)
        {
            return FormatVehicle(partRequest.Vehicle);
        }

        var fallback = customer?.Vehicles
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefault();

        return fallback == null ? "—" : FormatVehicle(fallback);
    }

    public static string FormatVehicle(Vehicle vehicle)
    {
        var label = $"{vehicle.Make} {vehicle.Model}".Trim();
        var year = vehicle.Year.HasValue && vehicle.Year > 0 ? $" ({vehicle.Year})" : "";
        var plate = string.IsNullOrWhiteSpace(vehicle.LicensePlate) ? "" : $" — {vehicle.LicensePlate}";
        return $"{label}{year}{plate}".Trim();
    }

    public static List<object> DeserializeHistory(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<object>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<object>>(json) ?? new List<object>();
        }
        catch
        {
            return new List<object>();
        }
    }

    public static object Map(PartRequest partRequest, Customer? customer) => new
    {
        id = partRequest.Id,
        customerId = partRequest.CustomerId,
        customerName = customer?.User?.Name ?? "",
        vehicleId = partRequest.VehicleId,
        partName = partRequest.PartName,
        partNumber = partRequest.PartNumber ?? "",
        quantity = partRequest.Quantity,
        category = partRequest.Category ?? "",
        urgency = partRequest.Urgency ?? "",
        description = partRequest.Description,
        customerNotes = partRequest.Description ?? "",
        status = partRequest.Status,
        staffComment = partRequest.StaffComment ?? "",
        commentHistory = DeserializeHistory(partRequest.StatusHistoryJson),
        vehicleInfo = BuildVehicleInfo(partRequest, customer),
        requestDate = partRequest.CreatedAt.ToString("yyyy-MM-dd"),
        createdAt = partRequest.CreatedAt
    };
}
