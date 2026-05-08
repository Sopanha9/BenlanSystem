using BenLanSystem.Models.DTOs;
using BenLanSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BenLanSystem.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class TripController(ITripService tripService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<TripSearchResultDto>>> Search([FromQuery] TripSearchDto search)
    {
        var result = await tripService.SearchAsync(search);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<TripDto>> GetById(long id)
    {
        var trip = await tripService.GetByIdAsync(id);
        if (trip is null) return NotFound();
        return Ok(trip);
    }

    [HttpGet("route/{routeId:int}")]
    public async Task<ActionResult<IEnumerable<TripDto>>> GetByRoute(int routeId)
    {
        var trips = await tripService.GetByRouteAsync(routeId);
        return Ok(trips);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<TripDto>> Create(TripCreateDto dto)
    {
        var trip = await tripService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = trip.Id }, trip);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<TripDto>> Update(long id, TripUpdateDto dto)
    {
        var trip = await tripService.UpdateAsync(id, dto);
        if (trip is null) return NotFound();
        return Ok(trip);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(long id)
    {
        var deleted = await tripService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}