using BenLanSystem.Models.DTOs;
using BenLanSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BenLanSystem.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class LocationController(ILocationService locationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LocationDto>>> GetAll()
    {
        var locations = await locationService.GetAllAsync();
        return Ok(locations);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LocationDto>> GetById(int id)
    {
        var loc = await locationService.GetByIdAsync(id);
        if (loc is null) return NotFound();
        return Ok(loc);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<LocationDto>> Create(LocationCreateDto dto)
    {
        var loc = await locationService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = loc.Id }, loc);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<LocationDto>> Update(int id, LocationUpdateDto dto)
    {
        var loc = await locationService.UpdateAsync(id, dto);
        if (loc is null) return NotFound();
        return Ok(loc);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await locationService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
