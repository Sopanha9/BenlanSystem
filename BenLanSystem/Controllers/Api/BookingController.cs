using BenLanSystem.Models.DTOs;
using BenLanSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BenLanSystem.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class BookingController(IBookingService bookingService) : ControllerBase
{
    [HttpGet("my")]
    [Authorize]
    public async Task<ActionResult<PagedResultDto<BookingDto>>> GetMyBookings([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();
        var result = await bookingService.GetByCustomerAsync(long.Parse(userId), page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [Authorize]
    public async Task<ActionResult<BookingDto>> GetById(long id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();

        var booking = await bookingService.GetByIdAsync(id);
        if (booking is null) return NotFound();
        if (!User.IsInRole("Admin") && !User.IsInRole("Staff") && booking.CustomerId != long.Parse(userId))
            return Forbid();

        return Ok(booking);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<BookingDto>> Create(BookingCreateDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();
        try
        {
            var booking = await bookingService.CreateAsync(long.Parse(userId), dto);
            return CreatedAtAction(nameof(GetById), new { id = booking.Id }, booking);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:long}/cancel")]
    [Authorize]
    public async Task<ActionResult<BookingDto>> Cancel(long id, [FromBody] BookingCancelDto? cancelDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();
        var booking = await bookingService.CancelAsync(id, long.Parse(userId), cancelDto);
        if (booking is null) return NotFound();
        return Ok(booking);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<BookingDto>> UpdateStatus(long id, BookingStatusUpdateDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();

        try
        {
            var booking = await bookingService.UpdateStatusAsync(id, long.Parse(userId), dto);
            if (booking is null) return NotFound();
            return Ok(booking);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("all")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await bookingService.GetAllAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("trip/{tripId:long}/seats")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<string>>> GetOccupiedSeats(long tripId)
    {
        var seats = await bookingService.GetOccupiedSeatNumbersAsync(tripId);
        return Ok(seats);
    }
}
