using System;
using System.Globalization;
using System.Linq;
using GearNXT_Backend.Data;
using GearNXT_Backend.DTOs.Customer;
using GearNXT_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GearNXT_Backend.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomerController : ControllerBase
{
    private readonly AppDbContext _db;

    public CustomerController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var customers = _db.Customers
            .Include(customer => customer.Vehicles)
            .Include(customer => customer.SalesInvoices)
            .ThenInclude(invoice => invoice.Items)
            .ThenInclude(item => item.Part)
            .OrderByDescending(customer => customer.CreatedAt)
            .ToList()
            .Select(MapCustomerSummary)
            .ToList();

        return Ok(customers);
    }

    [HttpPost]
    public IActionResult Create([FromBody] CustomerCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new { message = "Customer name is required." });
        }

        if (!string.IsNullOrWhiteSpace(request.Email) && _db.Customers.Any(customer => customer.Email != null && customer.Email.ToLower() == request.Email.ToLower()))
        {
            return Conflict(new { message = "Email already registered." });
        }

        var customer = new Customer
        {
            Name = request.FullName.Trim(),
            Email = NormalizeEmpty(request.Email),
            Phone = NormalizeEmpty(request.Phone),
            Address = NormalizeEmpty(request.Address),
            CreatedAt = DateTime.UtcNow
        };

        _db.Customers.Add(customer);
        _db.SaveChanges();

        if (request.Vehicle != null)
        {
            AddVehicleInternal(customer.Id, request.Vehicle);
        }

        var created = LoadCustomer(customer.Id) ?? customer;
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, MapCustomerSummary(created));
    }

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var customer = LoadCustomer(id);
        if (customer == null)
        {
            return NotFound(new { message = "Customer not found." });
        }

        return Ok(MapCustomerSummary(customer));
    }

    [HttpPut("{id:int}")]
    public IActionResult Update(int id, [FromBody] CustomerUpdateRequest request)
    {
        var customer = _db.Customers.Include(item => item.Vehicles).FirstOrDefault(item => item.Id == id);
        if (customer == null)
        {
            return NotFound(new { message = "Customer not found." });
        }

        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            customer.Name = request.FullName.Trim();
        }

        customer.Email = NormalizeEmpty(request.Email);
        customer.Phone = NormalizeEmpty(request.Phone);
        customer.Address = NormalizeEmpty(request.Address);

        if (request.Vehicle != null)
        {
            UpdatePrimaryVehicle(customer, request.Vehicle);
        }

        _db.SaveChanges();

        return Ok(MapCustomerSummary(LoadCustomer(id) ?? customer));
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var customer = _db.Customers.Include(item => item.SalesInvoices).FirstOrDefault(item => item.Id == id);
        if (customer == null)
        {
            return NotFound(new { message = "Customer not found." });
        }

        if (customer.SalesInvoices.Any())
        {
            return Conflict(new { message = "Cannot delete a customer with sales history." });
        }

        _db.Vehicles.RemoveRange(_db.Vehicles.Where(vehicle => vehicle.CustomerId == id));
        _db.Customers.Remove(customer);
        _db.SaveChanges();

        return NoContent();
    }

    [HttpGet("search")]
    public IActionResult Search([FromQuery] string q = "", [FromQuery] string? type = null)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(Array.Empty<object>());
        }

        var query = q.Trim().ToLower();

        var customers = _db.Customers
            .Include(customer => customer.Vehicles)
            .Include(customer => customer.SalesInvoices)
            .ThenInclude(invoice => invoice.Items)
            .ThenInclude(item => item.Part)
            .ToList()
            .Where(customer => MatchesSearch(customer, query, type))
            .Select(MapCustomerSummary)
            .ToList();

        return Ok(customers);
    }

    [HttpGet("{id:int}/vehicles")]
    public IActionResult GetVehicles(int id)
    {
        var customer = _db.Customers.Include(item => item.Vehicles).FirstOrDefault(item => item.Id == id);
        if (customer == null)
        {
            return NotFound(new { message = "Customer not found." });
        }

        return Ok(customer.Vehicles.OrderByDescending(vehicle => vehicle.CreatedAt).Select(MapVehicle));
    }

    [HttpPost("{id:int}/vehicles")]
    public IActionResult AddVehicle(int id, [FromBody] VehicleRequest request)
    {
        var customer = _db.Customers.FirstOrDefault(item => item.Id == id);
        if (customer == null)
        {
            return NotFound(new { message = "Customer not found." });
        }

        var vehicle = AddVehicleInternal(id, request);
        return CreatedAtAction(nameof(GetVehicles), new { id }, MapVehicle(vehicle));
    }

    [HttpGet("{id:int}/purchase-history")]
    [HttpGet("{id:int}/history")]
    public IActionResult GetPurchaseHistory(int id)
    {
        var customer = _db.Customers
            .Include(item => item.SalesInvoices)
            .ThenInclude(invoice => invoice.Items)
            .ThenInclude(item => item.Part)
            .FirstOrDefault(item => item.Id == id);

        if (customer == null)
        {
            return NotFound(new { message = "Customer not found." });
        }

        var history = customer.SalesInvoices
            .OrderByDescending(invoice => invoice.InvoiceDate)
            .Select(MapInvoiceHistory)
            .ToList();

        return Ok(history);
    }

    [HttpGet("/api/customer/profile")]
    public IActionResult LegacyProfile()
    {
        var customer = GetLegacyProfileCustomer();
        if (customer == null)
        {
            return NotFound(new { message = "Customer not found." });
        }

        return Ok(MapCustomerSummary(customer));
    }

    [HttpPut("/api/customer/profile")]
    public IActionResult LegacyUpdateProfile([FromBody] CustomerUpdateRequest request)
    {
        var customer = GetLegacyProfileCustomer();
        if (customer == null)
        {
            return NotFound(new { message = "Customer not found." });
        }

        customer.Name = string.IsNullOrWhiteSpace(request.FullName) ? customer.Name : request.FullName.Trim();
        customer.Phone = NormalizeEmpty(request.Phone) ?? customer.Phone;
        customer.Address = NormalizeEmpty(request.Address) ?? customer.Address;
        _db.SaveChanges();

        return Ok(new { ok = true });
    }

    private Customer? LoadCustomer(int id)
    {
        return _db.Customers
            .Include(customer => customer.Vehicles)
            .Include(customer => customer.SalesInvoices)
            .ThenInclude(invoice => invoice.Items)
            .ThenInclude(item => item.Part)
            .FirstOrDefault(customer => customer.Id == id);
    }

    private Customer? GetLegacyProfileCustomer()
    {
        if (Request.Headers.TryGetValue("X-Customer-Id", out var customerHeader) && int.TryParse(customerHeader, out var customerId))
        {
            return LoadCustomer(customerId);
        }

        return _db.Customers
            .Include(customer => customer.Vehicles)
            .Include(customer => customer.SalesInvoices)
            .ThenInclude(invoice => invoice.Items)
            .ThenInclude(item => item.Part)
            .OrderBy(customer => customer.Id)
            .FirstOrDefault();
    }

    private Vehicle AddVehicleInternal(int customerId, VehicleRequest request)
    {
        var vehicle = new Vehicle
        {
            CustomerId = customerId,
            Make = NormalizeEmpty(request.Make),
            Model = NormalizeEmpty(request.Model),
            Year = request.Year,
            LicensePlate = NormalizeEmpty(request.RegistrationNumber),
            VehicleNumber = NormalizeEmpty(request.VehicleNumber) ?? NormalizeEmpty(request.Vin),
            CreatedAt = DateTime.UtcNow
        };

        _db.Vehicles.Add(vehicle);
        _db.SaveChanges();
        return vehicle;
    }

    private void UpdatePrimaryVehicle(Customer customer, VehicleRequest request)
    {
        var vehicle = customer.Vehicles.OrderByDescending(item => item.CreatedAt).FirstOrDefault();
        if (vehicle == null)
        {
            AddVehicleInternal(customer.Id, request);
            return;
        }

        vehicle.Make = NormalizeEmpty(request.Make);
        vehicle.Model = NormalizeEmpty(request.Model);
        vehicle.Year = request.Year;
        vehicle.LicensePlate = NormalizeEmpty(request.RegistrationNumber);
        vehicle.VehicleNumber = NormalizeEmpty(request.VehicleNumber) ?? NormalizeEmpty(request.Vin);
    }

    private object MapCustomerSummary(Customer customer)
    {
        var vehicles = customer.Vehicles
            .OrderByDescending(vehicle => vehicle.CreatedAt)
            .Select(MapVehicle)
            .ToList();

        var primaryVehicle = vehicles.FirstOrDefault() ?? new
        {
            id = 0,
            customerId = 0,
            make = string.Empty,
            model = string.Empty,
            year = 0,
            registrationNumber = string.Empty,
            vin = string.Empty,
            licensePlate = string.Empty,
            vehicleNumber = string.Empty,
            createdAt = DateTime.UtcNow
        };

        var totalSpend = customer.SalesInvoices.Sum(invoice => invoice.GrandTotal);
        var creditBalance = customer.SalesInvoices.Where(invoice => string.Equals(invoice.PaymentStatus, "Credit", StringComparison.OrdinalIgnoreCase)).Sum(invoice => invoice.GrandTotal);
        var lastVisit = customer.SalesInvoices.OrderByDescending(invoice => invoice.InvoiceDate).Select(invoice => invoice.InvoiceDate).FirstOrDefault();

        return new
        {
            id = customer.Id,
            fullName = customer.Name,
            name = customer.Name,
            email = customer.Email ?? string.Empty,
            phone = customer.Phone ?? string.Empty,
            address = customer.Address ?? string.Empty,
            registeredDate = customer.CreatedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            createdAt = customer.CreatedAt,
            totalSpend,
            creditBalance,
            lastVisit = lastVisit == default ? string.Empty : lastVisit.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            vehicle = primaryVehicle,
            vehicles
        };
    }

    private object MapVehicle(Vehicle vehicle)
    {
        return new
        {
            id = vehicle.Id,
            customerId = vehicle.CustomerId,
            make = vehicle.Make ?? string.Empty,
            model = vehicle.Model ?? string.Empty,
            year = vehicle.Year ?? 0,
            registrationNumber = vehicle.LicensePlate ?? string.Empty,
            vin = vehicle.VehicleNumber ?? string.Empty,
            licensePlate = vehicle.LicensePlate ?? string.Empty,
            vehicleNumber = vehicle.VehicleNumber ?? string.Empty,
            createdAt = vehicle.CreatedAt
        };
    }

    private object MapInvoiceHistory(SalesInvoice invoice)
    {
        return new
        {
            id = invoice.Id,
            invoiceNumber = invoice.InvoiceNumber,
            date = invoice.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            type = "Sales Invoice",
            item = string.Join(", ", invoice.Items.Select(item => $"{item.PartName ?? item.Part?.Name ?? "Part"} x{item.Quantity}")),
            amount = invoice.GrandTotal,
            status = invoice.PaymentStatus,
            customerId = invoice.CustomerId,
            subtotal = invoice.TotalAmount,
            discount = invoice.DiscountAmount,
            discountApplied = invoice.DiscountApplied,
            grandTotal = invoice.GrandTotal,
            paymentStatus = invoice.PaymentStatus,
            items = invoice.Items.Select(item => new
            {
                id = item.Id,
                partId = item.PartId,
                partName = item.PartName ?? item.Part?.Name ?? string.Empty,
                quantity = item.Quantity,
                unitPrice = item.UnitPrice,
                total = item.Quantity * item.UnitPrice
            })
        };
    }

    private bool MatchesSearch(Customer customer, string query, string? type)
    {
        var vehicleMatches = customer.Vehicles.Any(vehicle =>
            (vehicle.LicensePlate ?? string.Empty).ToLower().Contains(query) ||
            (vehicle.VehicleNumber ?? string.Empty).ToLower().Contains(query));

        return type?.ToLower() switch
        {
            "name" => (customer.Name ?? string.Empty).ToLower().Contains(query),
            "phone" => (customer.Phone ?? string.Empty).ToLower().Contains(query),
            "id" => customer.Id.ToString(CultureInfo.InvariantCulture) == query,
            "vehicle" => vehicleMatches,
            _ => (customer.Name ?? string.Empty).ToLower().Contains(query)
                || (customer.Phone ?? string.Empty).ToLower().Contains(query)
                || customer.Id.ToString(CultureInfo.InvariantCulture) == query
                || vehicleMatches
        };
    }

    private static string? NormalizeEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}