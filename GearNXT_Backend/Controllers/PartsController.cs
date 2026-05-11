using System.Collections.Generic;
using System.Threading.Tasks;
using GearNXT_Backend.DTOs.Inventory;
using GearNXT_Backend.Models;
using GearNXT_Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace GearNXT_Backend.Controllers;

[ApiController]
[Route("api/parts")]
public class PartsController : ControllerBase
{
    private readonly PartsService _partsService;

    public PartsController(PartsService partsService)
    {
        _partsService = partsService;
    }

    [HttpPost]
    public async Task<ActionResult<Part>> CreatePart([FromBody] PartCreateDto dto)
    {
        var result = await _partsService.CreatePartAsync(dto);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Message);
        }

        return CreatedAtAction(nameof(GetPartById), new { id = result.Data!.Id }, result.Data);
    }

    [HttpGet]
    public async Task<ActionResult<List<Part>>> GetParts([FromQuery] string? category, [FromQuery] int? vendorId, [FromQuery] string? search)
    {
        var result = await _partsService.GetPartsAsync(category, vendorId, search);
        return Ok(result.Data);
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<List<Part>>> GetLowStockParts()
    {
        var result = await _partsService.GetLowStockPartsAsync();
        return Ok(result.Data);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Part>> GetPartById(int id)
    {
        var result = await _partsService.GetPartByIdAsync(id);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Message);
        }

        return Ok(result.Data);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdatePart(int id, [FromBody] PartUpdateDto dto)
    {
        var result = await _partsService.UpdatePartAsync(id, dto);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Message);
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePart(int id)
    {
        var result = await _partsService.DeletePartAsync(id);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Message);
        }

        return NoContent();
    }
}
