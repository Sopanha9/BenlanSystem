using BenLanSystem.Models.DTOs;

namespace BenLanSystem.Services.Interfaces;

public interface ILocationService
{
    Task<IEnumerable<LocationDto>> GetAllAsync();
    Task<LocationDto?> GetByIdAsync(int id);
    Task<LocationDto> CreateAsync(LocationCreateDto dto);
    Task<LocationDto?> UpdateAsync(int id, LocationUpdateDto dto);
    Task<bool> DeleteAsync(int id);
}