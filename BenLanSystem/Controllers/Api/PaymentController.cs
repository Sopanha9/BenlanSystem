using BenLanSystem.Models.DTOs;
using BenLanSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BenLanSystem.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class PaymentController(IPaymentService paymentService) : ControllerBase
{
    [HttpGet("{id:long}")]
    public async Task<ActionResult<PaymentDto>> GetById(long id)
    {
        var payment = await paymentService.GetByIdAsync(id);
        if (payment is null) return NotFound();
        return Ok(payment);
    }

    [HttpGet("booking/{bookingId:long}")]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetByBooking(long bookingId)
    {
        var payments = await paymentService.GetByBookingAsync(bookingId);
        return Ok(payments);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<PaymentDto>> Create(PaymentCreateDto dto)
    {
        var payment = await paymentService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = payment.Id }, payment);
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
    }
}