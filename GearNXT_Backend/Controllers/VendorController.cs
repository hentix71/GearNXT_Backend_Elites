using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using GearNXT_Backend.Models;
using GearNXT_Backend.DTOs.Vendor;
using GearNXT_Backend.Services;
using Superpower.Model;

namespace GearNXT_Backend.Controllers;

[Authorize(Roles = "Admin, Staff")]
[Route("api/vendors")]
[ApiController]
public class VendorController : ControllerBase
{
    private readonly VendorService _vendorService;

    public VendorController(VendorService vendorService)
    {
        _vendorService = vendorService;
    }

    // POST /api/vendors/register
    [HttpPost("register")]
    public async Task<IActionResult> RegisterVendor([FromBody] RegisterVendorDto request)
    {
        var result = await _vendorService.RegisterVendorAsync(request);
        if (result == null)
            return BadRequest("Failed to register vendor.");

        return Ok(result);
    }

    // GET /api/vendors/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetVendorById(int id)
    {
        var result = await _vendorService.GetVendorByIdAsync(id);
        if (result == null)
            return NotFound("Vendor not found.");
        return Ok(result);
    }

    // PATCH /api/vendors/{id}/toggle-active
    [HttpPatch("{id}/toggle-active")]
    public async Task<IActionResult> ToggleVendorIsActive(int id)
    {
        var result = await _vendorService.ToggleVendorIsActiveAsync(id);
        if (result == null)
            return NotFound("Vendor not found.");

        return Ok(result);
    }

    // GET /api/vendors
    [HttpGet]
    public async Task<IActionResult> ListVendors()
    {
        var result = await _vendorService.ListVendorsAsync();
        if (result == null || !result.Any())
            return NotFound("No vendors found.");

        return Ok(result);
    }

    // GET /api/vendors/active
    [HttpGet("active")]
    public async Task<IActionResult> ListActiveVendors()
    {
        var result = await _vendorService.ListActiveVendorsAsync();
        if (result == null || !result.Any())
            return NotFound("No active vendors found.");

        return Ok(result);
    }

    // PATCH /api/vendors/{id}/update
    [HttpPatch("{id}/update")]
    public async Task<IActionResult> UpdateVendor(int id, [FromBody] VendorDto updateRequest)
    {

        var result = await _vendorService.UpdateVendorAsync(id, updateRequest);
        if (result == null)
            return BadRequest("Error occurred while updating vendor.");
        return Ok(result);
    }
}
