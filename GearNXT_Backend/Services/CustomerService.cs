using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GearNXT_Backend.Data;
using GearNXT_Backend.DTOs.Customer;
using GearNXT_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace GearNXT_Backend.Services;

public class CustomerService
{
    private readonly AppDbContext _db;

    public CustomerService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Customer>> ListAsync()
    {
        return await _db.Customers
            .Include(c => c.Vehicles)
            .Include(c => c.User)
            .Include(c => c.SalesInvoices)
                .ThenInclude(si => si.Items)
                .ThenInclude(i => i.Part)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Customer> CreateAsync(CustomerCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ArgumentException("Customer name is required.");

        if (string.IsNullOrWhiteSpace(request.Phone))
            throw new ArgumentException("Phone number is required.");

        var phone = request.Phone.Trim();
        var fullName = request.FullName.Trim();
        User? user = null;
        var email = request.Email?.Trim();

        if (!string.IsNullOrWhiteSpace(email))
        {
            user = await _db.Users.FirstOrDefaultAsync(
                u => u.Email != null && u.Email.ToLower() == email.ToLower());

            if (user != null && await _db.Customers.AnyAsync(c => c.UserId == user.Id))
            {
                throw new InvalidOperationException("A customer profile already exists for this email.");
            }
        }
        else
        {
            email = await GenerateWalkInEmailAsync();
        }

        if (user == null)
        {
            var phoneTaken = await _db.Users.AnyAsync(
                u => u.Role == Role.Customer && u.Phone != null && u.Phone == phone);
            if (phoneTaken)
            {
                throw new InvalidOperationException("Phone number already registered.");
            }

            // If the request provided a password (staff/admin creating a customer), use it.
            var rawPassword = string.IsNullOrWhiteSpace(request.Password)
                ? Guid.NewGuid().ToString("N")
                : request.Password!;

            user = new User
            {
                Name = fullName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword),
                Role = Role.Customer,
                Phone = phone,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }
        else
        {
            user.Name = fullName;
            user.Phone = phone;
        }

        var customer = new Customer
        {
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            TotalDiscountEarned = 0.00m,
            TotalSpent = 0.00m,
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        if (request.Vehicle != null)
        {
            await EnsureUniqueLicensePlateAsync(request.Vehicle.RegistrationNumber);
            await AddVehicleAsync(customer.Id, request.Vehicle);
        }

        return customer;
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        return await _db.Customers
            .Include(c => c.Vehicles)
            .Include(c => c.User)
            .Include(c => c.SalesInvoices)
                .ThenInclude(si => si.Items)
                .ThenInclude(i => i.Part)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Customer?> GetByUserIdAsync(int userId)
    {
        return await _db.Customers
            .Include(c => c.Vehicles)
            .Include(c => c.User)
            .Include(c => c.SalesInvoices)
                .ThenInclude(si => si.Items)
                .ThenInclude(i => i.Part)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<Customer?> UpdateAsync(int id, CustomerUpdateRequest request)
    {
        var customer = await _db.Customers.Include(c => c.Vehicles).FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null) return null;

        // Update linked user profile when changing personal/contact fields.
        if (customer.UserId.HasValue)
        {
            var user = await _db.Users.FindAsync(customer.UserId.Value);
            if (user != null)
            {
                if (!string.IsNullOrWhiteSpace(request.FullName))
                    user.Name = request.FullName.Trim();

                if (!string.IsNullOrWhiteSpace(request.Email))
                    user.Email = request.Email.Trim();

                user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? user.Phone : request.Phone.Trim();
                user.UpdatedAt = DateTime.UtcNow;
            }
        }
        else
        {
            // Try linking to an existing user by email if present
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email.Trim());
                if (existingUser != null)
                {
                    customer.UserId = existingUser.Id;
                }
            }
        }

        var addressValue = request.AddressLine ?? request.Address;
        if (addressValue != null)
        {
            customer.AddressLine = string.IsNullOrWhiteSpace(addressValue) ? null : addressValue.Trim();
        }

        if (request.City != null)
        {
            customer.City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim();
        }

        if (request.District != null)
        {
            customer.District = string.IsNullOrWhiteSpace(request.District) ? null : request.District.Trim();
        }

        if (request.PreferredContactMethod != null)
        {
            customer.PreferredContactMethod = string.IsNullOrWhiteSpace(request.PreferredContactMethod)
                ? null
                : request.PreferredContactMethod.Trim();
        }

        if (request.Vehicle != null)
        {
            var primary = customer.Vehicles.OrderByDescending(v => v.CreatedAt).FirstOrDefault();
            if (primary == null)
            {
                await AddVehicleAsync(customer.Id, request.Vehicle);
            }
            else
            {
                primary.Make = string.IsNullOrWhiteSpace(request.Vehicle.Make) ? primary.Make : request.Vehicle.Make;
                primary.Model = string.IsNullOrWhiteSpace(request.Vehicle.Model) ? primary.Model : request.Vehicle.Model;
                primary.Year = request.Vehicle.Year ?? primary.Year;
                primary.LicensePlate = string.IsNullOrWhiteSpace(request.Vehicle.RegistrationNumber) ? primary.LicensePlate : request.Vehicle.RegistrationNumber;
                primary.VehicleNumber = string.IsNullOrWhiteSpace(request.Vehicle.VehicleNumber) ? primary.VehicleNumber : request.Vehicle.VehicleNumber ?? request.Vehicle.Vin;
                primary.Color = string.IsNullOrWhiteSpace(request.Vehicle.Color) ? primary.Color : request.Vehicle.Color.Trim();
                primary.FuelType = string.IsNullOrWhiteSpace(request.Vehicle.FuelType) ? primary.FuelType : request.Vehicle.FuelType.Trim();
            }
        }

        await _db.SaveChangesAsync();
        return customer;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var customer = await _db.Customers.Include(c => c.SalesInvoices).FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null) return false;
        if (customer.SalesInvoices.Any()) throw new InvalidOperationException("Cannot delete a customer with sales history.");

        var vehicles = _db.Vehicles.Where(v => v.CustomerId == id);
        _db.Vehicles.RemoveRange(vehicles);
        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<Customer>> SearchAsync(string q, string? type)
    {
        if (string.IsNullOrWhiteSpace(q)) return new List<Customer>();
        var query = q.Trim().ToLower();

        var customers = await _db.Customers
            .Include(c => c.Vehicles)
            .Include(c => c.User)
            .Include(c => c.SalesInvoices)
                .ThenInclude(si => si.Items)
                .ThenInclude(i => i.Part)
            .ToListAsync();

        return customers.Where(c => MatchesSearch(c, query, type)).ToList();
    }

    public async Task<List<Vehicle>> GetVehiclesAsync(int customerId)
    {
        var customer = await _db.Customers.Include(c => c.Vehicles).FirstOrDefaultAsync(c => c.Id == customerId);
        if (customer == null) return new List<Vehicle>();
        return customer.Vehicles
            .OrderByDescending(v => v.IsPrimary)
            .ThenByDescending(v => v.CreatedAt)
            .ToList();
    }

    public async Task<Vehicle> AddVehicleAsync(int customerId, VehicleRequest request)
    {
        await EnsureUniqueLicensePlateAsync(request.RegistrationNumber);

        var existingCount = await _db.Vehicles.CountAsync(v => v.CustomerId == customerId);
        var makePrimary = request.IsPrimary == true || existingCount == 0;

        if (makePrimary)
        {
            await ClearPrimaryVehicleAsync(customerId);
        }

        var vehicle = new Vehicle
        {
            CustomerId = customerId,
            Make = string.IsNullOrWhiteSpace(request.Make) ? null : request.Make.Trim(),
            Model = string.IsNullOrWhiteSpace(request.Model) ? null : request.Model.Trim(),
            Year = request.Year ?? 0,
            LicensePlate = string.IsNullOrWhiteSpace(request.RegistrationNumber) ? null : request.RegistrationNumber.Trim(),
            VehicleNumber = string.IsNullOrWhiteSpace(request.VehicleNumber) ? (string.IsNullOrWhiteSpace(request.Vin) ? null : request.Vin.Trim()) : request.VehicleNumber.Trim(),
            Color = string.IsNullOrWhiteSpace(request.Color) ? null : request.Color.Trim(),
            FuelType = string.IsNullOrWhiteSpace(request.FuelType) ? null : request.FuelType.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            IsPrimary = makePrimary,
            CreatedAt = DateTime.UtcNow
        };

        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync();
        return vehicle;
    }

    public async Task<List<SalesInvoice>> GetPurchaseHistoryAsync(int customerId)
    {
        var customer = await _db.Customers
            .Include(c => c.SalesInvoices)
                .ThenInclude(si => si.Items)
                .ThenInclude(i => i.Part)
            .FirstOrDefaultAsync(c => c.Id == customerId);

        if (customer == null) return new List<SalesInvoice>();
        return customer.SalesInvoices.OrderByDescending(si => si.InvoiceDate).ToList();
    }

    private async Task<string> GenerateWalkInEmailAsync()
    {
        string email;
        do
        {
            email = $"walkin-{Guid.NewGuid():N}@gearnxt.local";
        } while (await _db.Users.AnyAsync(u => u.Email == email));

        return email;
    }

    public async Task<Vehicle?> UpdateVehicleAsync(int customerId, int vehicleId, VehicleRequest request)
    {
        var vehicle = await _db.Vehicles
            .FirstOrDefaultAsync(v => v.Id == vehicleId && v.CustomerId == customerId);
        if (vehicle == null)
        {
            return null;
        }

        await EnsureUniqueLicensePlateAsync(request.RegistrationNumber, vehicleId);

        vehicle.Make = string.IsNullOrWhiteSpace(request.Make) ? vehicle.Make : request.Make.Trim();
        vehicle.Model = string.IsNullOrWhiteSpace(request.Model) ? vehicle.Model : request.Model.Trim();
        vehicle.Year = request.Year ?? vehicle.Year;
        vehicle.LicensePlate = string.IsNullOrWhiteSpace(request.RegistrationNumber)
            ? vehicle.LicensePlate
            : request.RegistrationNumber.Trim();
        vehicle.VehicleNumber = string.IsNullOrWhiteSpace(request.VehicleNumber)
            ? (string.IsNullOrWhiteSpace(request.Vin) ? vehicle.VehicleNumber : request.Vin.Trim())
            : request.VehicleNumber.Trim();
        vehicle.Color = string.IsNullOrWhiteSpace(request.Color) ? vehicle.Color : request.Color.Trim();
        vehicle.FuelType = string.IsNullOrWhiteSpace(request.FuelType) ? vehicle.FuelType : request.FuelType.Trim();
        vehicle.Notes = string.IsNullOrWhiteSpace(request.Notes) ? vehicle.Notes : request.Notes.Trim();

        if (request.IsPrimary == true)
        {
            await ClearPrimaryVehicleAsync(customerId);
            vehicle.IsPrimary = true;
        }

        await _db.SaveChangesAsync();
        return vehicle;
    }

    public async Task<Vehicle?> SetPrimaryVehicleAsync(int customerId, int vehicleId)
    {
        var vehicle = await _db.Vehicles
            .FirstOrDefaultAsync(v => v.Id == vehicleId && v.CustomerId == customerId);
        if (vehicle == null)
        {
            return null;
        }

        await ClearPrimaryVehicleAsync(customerId);
        vehicle.IsPrimary = true;
        await _db.SaveChangesAsync();
        return vehicle;
    }

    public async Task<bool> DeleteVehicleAsync(int customerId, int vehicleId)
    {
        var vehicle = await _db.Vehicles
            .FirstOrDefaultAsync(v => v.Id == vehicleId && v.CustomerId == customerId);
        if (vehicle == null)
        {
            return false;
        }

        var wasPrimary = vehicle.IsPrimary;
        _db.Vehicles.Remove(vehicle);
        await _db.SaveChangesAsync();

        if (wasPrimary)
        {
            var replacement = await _db.Vehicles
                .Where(v => v.CustomerId == customerId)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync();
            if (replacement != null)
            {
                replacement.IsPrimary = true;
                await _db.SaveChangesAsync();
            }
        }

        return true;
    }

    private async Task ClearPrimaryVehicleAsync(int customerId)
    {
        var primaries = await _db.Vehicles
            .Where(v => v.CustomerId == customerId && v.IsPrimary)
            .ToListAsync();
        foreach (var v in primaries)
        {
            v.IsPrimary = false;
        }
    }

    private async Task EnsureUniqueLicensePlateAsync(string? registrationNumber, int? excludeVehicleId = null)
    {
        if (string.IsNullOrWhiteSpace(registrationNumber))
        {
            return;
        }

        var plate = registrationNumber.Trim().ToLower();
        var exists = await _db.Vehicles.AnyAsync(
            v => v.LicensePlate != null &&
                 v.LicensePlate.ToLower() == plate &&
                 (!excludeVehicleId.HasValue || v.Id != excludeVehicleId.Value));
        if (exists)
        {
            throw new InvalidOperationException(
                $"Vehicle registration '{registrationNumber.Trim()}' is already on file.");
        }
    }

    private bool MatchesSearch(Customer customer, string query, string? type)
    {
        var vehicleMatches = customer.Vehicles.Any(vehicle =>
            (vehicle.LicensePlate ?? string.Empty).ToLower().Contains(query) ||
            (vehicle.VehicleNumber ?? string.Empty).ToLower().Contains(query));

        var name = customer.User?.Name ?? string.Empty;
        var phone = customer.User?.Phone ?? string.Empty;

        return type?.ToLower() switch
        {
            "name" => name.ToLower().Contains(query),
            "phone" => phone.ToLower().Contains(query),
            "id" => customer.Id.ToString() == query,
            "vehicle" => vehicleMatches,
            _ => name.ToLower().Contains(query)
                || phone.ToLower().Contains(query)
                || customer.Id.ToString() == query
                || vehicleMatches
        };
    }
}
