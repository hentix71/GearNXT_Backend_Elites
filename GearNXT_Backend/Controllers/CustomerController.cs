using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using GearNXT_Backend.Data;
using GearNXT_Backend.DTOs.Customer;
using GearNXT_Backend.Models;
using GearNXT_Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace GearNXT_Backend.Controllers
{
    [ApiController]
    [Route("api/customers")]
    [Authorize]
    public class CustomerController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly CustomerService _service;
        private readonly IMapper _mapper;
        private readonly NotificationService _notificationService;

        public CustomerController(AppDbContext db, CustomerService service, IMapper mapper, NotificationService notificationService)
        {
            _db = db;
            _service = service;
            _mapper = mapper;
            _notificationService = notificationService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAll()
        {
            var customers = await _service.ListAsync();
            var dtos = customers.Select(c => _mapper.Map<CustomerSummaryDto>(c));
            return Ok(dtos);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Create([FromBody] CustomerCreateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
                return BadRequest(new { message = "Customer name is required." });

            if (string.IsNullOrWhiteSpace(request.Phone))
                return BadRequest(new { message = "Phone number is required." });

            if (!string.IsNullOrWhiteSpace(request.Email) && await _db.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == request.Email.ToLower()))
            {
                var linkedCustomer = await _db.Customers.AnyAsync(
                    c => c.User != null && c.User.Email != null && c.User.Email.ToLower() == request.Email.ToLower());
                if (linkedCustomer)
                    return Conflict(new { message = "A customer profile already exists for this email." });
            }

            try
            {
                // If caller is authenticated and is staff/admin, require password fields
                var isPrivilegedCaller = User?.Identity?.IsAuthenticated == true &&
                    (User.IsInRole("Admin") || User.IsInRole("Staff"));

                if (isPrivilegedCaller)
                {
                    if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
                        return BadRequest(new { message = "Set a temporary password of at least 8 characters." });
                    if (request.Password != request.ConfirmPassword)
                        return BadRequest(new { message = "Passwords do not match." });
                }

                var created = await _service.CreateAsync(request);
                var loaded = await _service.GetByIdAsync(created.Id) ?? created;

                if (loaded.UserId.HasValue)
                {
                    await _notificationService.NotifyUserAsync(
                        loaded.UserId.Value,
                        "Account",
                        "Your customer profile has been created and is ready for use.");
                }

                return CreatedAtAction(nameof(GetById), new { id = loaded.Id }, _mapper.Map<CustomerSummaryDto>(loaded));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var customer = await _service.GetByIdAsync(id);
            if (customer == null) return NotFound(new { message = "Customer not found." });

            if (!CallerIsAdminOrStaff() && customer.UserId != GetCallerId())
                return Forbid();

            return Ok(_mapper.Map<CustomerSummaryDto>(customer));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CustomerUpdateRequest request)
        {
            var customer = await _service.GetByIdAsync(id);
            if (customer == null) return NotFound(new { message = "Customer not found." });

            if (!CallerIsAdminOrStaff() && customer.UserId != GetCallerId())
                return Forbid();

            var updated = await _service.UpdateAsync(id, request);
            if (updated == null) return NotFound(new { message = "Customer not found." });

            var loaded = await _service.GetByIdAsync(id) ?? updated;
            return Ok(_mapper.Map<CustomerSummaryDto>(loaded));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var ok = await _service.DeleteAsync(id);
                if (!ok) return NotFound(new { message = "Customer not found." });
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpGet("search")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Search([FromQuery] string q = "", [FromQuery] string? type = null)
        {
            if (string.IsNullOrWhiteSpace(q)) return Ok(Array.Empty<object>());
            var results = await _service.SearchAsync(q, type);
            return Ok(results.Select(c => _mapper.Map<CustomerSummaryDto>(c)));
        }

        [HttpGet("{id:int}/vehicles")]
        public async Task<IActionResult> GetVehicles(int id)
        {
            var customer = await _service.GetByIdAsync(id);
            if (customer == null) return NotFound(new { message = "Customer not found." });

            if (!CallerIsAdminOrStaff() && customer.UserId != GetCallerId())
                return Forbid();

            var vehicles = await _service.GetVehiclesAsync(id);
            return Ok(vehicles.OrderByDescending(v => v.CreatedAt).Select(v => _mapper.Map<VehicleDto>(v)));
        }

        [HttpPost("{id:int}/vehicles")]
        public async Task<IActionResult> AddVehicle(int id, [FromBody] VehicleRequest request)
        {
            var customer = await _service.GetByIdAsync(id);
            if (customer == null) return NotFound(new { message = "Customer not found." });

            if (!CallerIsAdminOrStaff() && customer.UserId != GetCallerId())
                return Forbid();

            var vehicle = await _service.AddVehicleAsync(id, request);
            return CreatedAtAction(nameof(GetVehicles), new { id }, _mapper.Map<VehicleDto>(vehicle));
        }

        [HttpGet("{id:int}/purchase-history")]
        [HttpGet("{id:int}/history")]
        public async Task<IActionResult> GetPurchaseHistory(int id)
        {
            var customer = await _service.GetByIdAsync(id);
            if (customer == null) return NotFound(new { message = "Customer not found." });

            if (!CallerIsAdminOrStaff() && customer.UserId != GetCallerId())
                return Forbid();

            var invoices = await _service.GetPurchaseHistoryAsync(id);
            return Ok(invoices.OrderByDescending(i => i.InvoiceDate).Select(i => _mapper.Map<InvoiceDto>(i)));
        }

        [HttpGet("/api/customer/profile")]
        public async Task<IActionResult> LegacyProfile()
        {
            var callerId = GetCallerId();
            Customer? customer = null;
            if (callerId != null)
            {
                customer = await _service.GetByUserIdAsync(callerId.Value);
            }

            customer ??= GetLegacyProfileCustomer();
            if (customer == null) return NotFound(new { message = "Customer not found." });
            return Ok(_mapper.Map<CustomerSummaryDto>(customer));
        }

        [HttpPut("/api/customer/profile")]
        public async Task<IActionResult> LegacyUpdateProfile([FromBody] CustomerUpdateRequest request)
        {
            var callerId = GetCallerId();
            if (callerId == null) return Unauthorized();

            var customer = await _service.GetByUserIdAsync(callerId.Value) ?? GetLegacyProfileCustomer();
            if (customer == null) return NotFound(new { message = "Customer not found." });

            var updated = await _service.UpdateAsync(customer.Id, request);
            if (updated == null) return NotFound(new { message = "Customer not found." });
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

        private int? GetCallerId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out var id)) return id;
            return null;
        }

        private bool CallerIsAdminOrStaff()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrWhiteSpace(role)) return false;
            return role.Equals("Admin", StringComparison.OrdinalIgnoreCase) || role.Equals("Staff", StringComparison.OrdinalIgnoreCase);
        }
    }
}
