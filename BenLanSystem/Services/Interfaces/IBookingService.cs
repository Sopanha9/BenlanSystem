using BenLanSystem.Models.DTOs;

namespace BenLanSystem.Services.Interfaces;

public interface IBookingService
{
    Task<PagedResultDto<BookingDto>> GetByCustomerAsync(long customerId, int page = 1, int pageSize = 10);
    Task<BookingDto?> GetByIdAsync(long id);
    Task<BookingDto> CreateAsync(long customerId, BookingCreateDto dto);
    Task<BookingDto?> CancelAsync(long id, long customerId, BookingCancelDto? cancelDto);
    Task<IEnumerable<BookingDto>> GetAllAsync(int page = 1, int pageSize = 20);
}