using BenLanSystem.Models.DTOs;

namespace BenLanSystem.Services.Interfaces;

public interface ITripService
{
    Task<PagedResultDto<TripSearchResultDto>> SearchAsync(TripSearchDto search);
    Task<TripDto?> GetByIdAsync(long id);
    Task<TripDto> CreateAsync(TripCreateDto dto);
    Task<TripDto?> UpdateAsync(long id, TripUpdateDto dto);
    Task<bool> DeleteAsync(long id);
    Task<IEnumerable<TripDto>> GetByRouteAsync(int routeId);
}