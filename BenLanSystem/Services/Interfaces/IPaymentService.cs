using BenLanSystem.Models.DTOs;

namespace BenLanSystem.Services.Interfaces;

public interface IPaymentService
{
    Task<PaymentDto> CreateAsync(PaymentCreateDto dto);
    Task<PaymentDto?> GetByIdAsync(long id);
    Task<IEnumerable<PaymentDto>> GetByBookingAsync(long bookingId);
    Task<IEnumerable<PaymentDto>> GetAllAsync(int page = 1, int pageSize = 50);
    Task<PaymentDto> MarkAsPaidAsync(long id, PaymentMarkPaidDto dto);
    Task<PaymentDto> RefundAsync(long id, PaymentRefundDto dto);
}
