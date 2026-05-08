using BenLanSystem.Models.DTOs;

namespace BenLanSystem.Services.Interfaces;

public interface IStaffService
{
    Task<IEnumerable<StaffDto>> GetAllAsync();
    Task<StaffDto?> GetByIdAsync(long id);
    Task<StaffDto> CreateAsync(StaffCreateDto dto);
    Task<StaffDto?> UpdateAsync(long id, StaffUpdateDto dto);
}