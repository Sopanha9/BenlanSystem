using BenLanSystem.Models.DTOs;
using BenLanSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BenLanSystem.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class StaffController(IStaffService staffService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<IEnumerable<StaffDto>>> GetAll()
    {
        var staff = await staffService.GetAllAsync();
        return Ok(staff);
    }

    [HttpGet("{id:long}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<StaffDto>> GetById(long id)
    {
        var s = await staffService.GetByIdAsync(id);
        if (s is null) return NotFound();
        return Ok(s);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<StaffDto>> Create(StaffCreateDto dto)
    {
        try
        {
            var s = await staffService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = s.Id }, s);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:long}")]
    [Authorize]
    public async Task<ActionResult<StaffDto>> Update(long id, StaffUpdateDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();

        // Staff can only update their own profile unless they are Admin
        if (!User.IsInRole("Admin") && long.Parse(userId) != id)
            return Forbid();

        var s = await staffService.UpdateAsync(id, dto);
        if (s is null) return NotFound();
        return Ok(s);
    }
}
