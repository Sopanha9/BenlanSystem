using BenLanSystem.Models.DTOs;
using BenLanSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BenLanSystem.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class VehicleController(IVehicleService vehicleService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<VehicleDto>>> GetAll()
    {
        var vehicles = await vehicleService.GetAllAsync();
        return Ok(vehicles);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VehicleDto>> GetById(int id)
    {
        var vehicle = await vehicleService.GetByIdAsync(id);
        if (vehicle is null) return NotFound();
        return Ok(vehicle);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<VehicleDto>> Create(VehicleCreateDto dto)
    {
        var vehicle = await vehicleService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = vehicle.Id }, vehicle);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<VehicleDto>> Update(int id, VehicleUpdateDto dto)
    {
        var vehicle = await vehicleService.UpdateAsync(id, dto);
        if (vehicle is null) return NotFound();
        return Ok(vehicle);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await vehicleService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
