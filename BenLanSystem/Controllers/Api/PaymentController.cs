using BenLanSystem.Data;
using BenLanSystem.Models.DTOs;
using BenLanSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BenLanSystem.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class PaymentController(IPaymentService paymentService, ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var payments = await paymentService.GetAllAsync(page, pageSize);
        return Ok(payments);
    }

    [HttpGet("{id:long}")]
    [Authorize]
    public async Task<ActionResult<PaymentDto>> GetById(long id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();

        var payment = await paymentService.GetByIdAsync(id);
        if (payment is null) return NotFound();
        if (!User.IsInRole("Admin") && !User.IsInRole("Staff"))
        {
            var booking = await db.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == payment.BookingId);
            if (booking is null || booking.CustomerId != long.Parse(userId)) return Forbid();
        }

        return Ok(payment);
    }

    [HttpGet("booking/{bookingId:long}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetByBooking(long bookingId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();

        if (!User.IsInRole("Admin") && !User.IsInRole("Staff"))
        {
            var booking = await db.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookingId);
            if (booking is null || booking.CustomerId != long.Parse(userId)) return Forbid();
        }

        var payments = await paymentService.GetByBookingAsync(bookingId);
        return Ok(payments);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<PaymentDto>> Create(PaymentCreateDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();

        // Verify booking ownership (skip for Admin/Staff)
        if (!User.IsInRole("Admin") && !User.IsInRole("Staff"))
        {
            var booking = await db.Bookings.FindAsync(dto.BookingId);
            if (booking is null || booking.CustomerId != long.Parse(userId))
                return Forbid();
        }

        try
        {
            var payment = await paymentService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = payment.Id }, payment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:long}/mark-paid")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<PaymentDto>> MarkAsPaid(long id, [FromBody] PaymentMarkPaidDto dto)
    {
        try
        {
            var payment = await paymentService.MarkAsPaidAsync(id, dto);
            return Ok(payment);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:long}/refund")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PaymentDto>> Refund(long id, [FromBody] PaymentRefundDto dto)
    {
        try
        {
            var payment = await paymentService.RefundAsync(id, dto);
            return Ok(payment);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
