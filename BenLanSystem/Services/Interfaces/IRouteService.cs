using BenLanSystem.Models.DTOs;

namespace BenLanSystem.Services.Interfaces;

public interface IRouteService
{
    Task<IEnumerable<RouteDto>> GetAllAsync();
    Task<RouteDto?> GetByIdAsync(int id);
    Task<RouteDto> CreateAsync(RouteCreateDto dto);
    Task<RouteDto?> UpdateAsync(int id, RouteUpdateDto dto);
    Task<bool> DeleteAsync(int id);
}