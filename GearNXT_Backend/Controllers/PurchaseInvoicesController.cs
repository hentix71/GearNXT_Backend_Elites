using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GearNXT_Backend.DTOs.Inventory;
using GearNXT_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GearNXT_Backend.Controllers;

[ApiController]
[Route("api/purchase-invoices")]
[Authorize(Roles = "Admin")]
public class PurchaseInvoicesController : ControllerBase
{
    private readonly PurchaseInvoiceService _invoiceService;

    public PurchaseInvoicesController(PurchaseInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseInvoiceDto>> CreateInvoice([FromBody] PurchaseInvoiceCreateDto dto)
    {
        // Use authenticated subject if available for audit fields.
        var createdBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _invoiceService.CreateInvoiceAsync(dto, createdBy);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Message);
        }

        return CreatedAtAction(nameof(GetInvoiceById), new { id = result.Data!.Id }, result.Data);
    }

    [HttpGet]
    public async Task<ActionResult<List<PurchaseInvoiceDto>>> GetInvoices(
        [FromQuery] int? vendorId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // Optional filters and paging for invoice history views.
        var result = await _invoiceService.GetInvoicesAsync(vendorId, from, to, page, pageSize);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Message);
        }

        return Ok(result.Data);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PurchaseInvoiceDto>> GetInvoiceById(int id)
    {
        var result = await _invoiceService.GetInvoiceByIdAsync(id);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Message);
        }

        return Ok(result.Data);
    }

    [HttpPatch("{id:int}/cancel")]
    public async Task<ActionResult<PurchaseInvoiceDto>> CancelInvoice(int id)
    {
        var result = await _invoiceService.CancelInvoiceAsync(id);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Message);
        }

        return Ok(result.Data);
    }
}
