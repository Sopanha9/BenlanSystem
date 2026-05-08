using BenLanSystem.Models.DTOs;

namespace BenLanSystem.Services.Interfaces;

public interface IVehicleService
{
    Task<IEnumerable<VehicleDto>> GetAllAsync();
    Task<VehicleDto?> GetByIdAsync(int id);
    Task<VehicleDto> CreateAsync(VehicleCreateDto dto);
    Task<VehicleDto?> UpdateAsync(int id, VehicleUpdateDto dto);
    Task<bool> DeleteAsync(int id);
}