using BenLanSystem.Models.DTOs;
using BenLanSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BenLanSystem.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class RouteController(IRouteService routeService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RouteDto>>> GetAll()
    {
        var routes = await routeService.GetAllAsync();
        return Ok(routes);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RouteDto>> GetById(int id)
    {
        var route = await routeService.GetByIdAsync(id);
        if (route is null) return NotFound();
        return Ok(route);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<RouteDto>> Create(RouteCreateDto dto)
    {
        var route = await routeService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = route.Id }, route);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<RouteDto>> Update(int id, RouteUpdateDto dto)
    {
        var route = await routeService.UpdateAsync(id, dto);
        if (route is null) return NotFound();
        return Ok(route);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await routeService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
