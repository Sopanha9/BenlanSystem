using BenLanSystem.Models.DTOs;

namespace BenLanSystem.Services.Interfaces;

public interface IPaymentService
{
    Task<PaymentDto> CreateAsync(PaymentCreateDto dto);
    Task<PaymentDto?> GetByIdAsync(long id);
    Task<IEnumerable<PaymentDto>> GetByBookingAsync(long bookingId);
    Task<PaymentDto> MarkAsPaidAsync(long id, PaymentMarkPaidDto dto);
    Task<PaymentDto> RefundAsync(long id, PaymentRefundDto dto);
}