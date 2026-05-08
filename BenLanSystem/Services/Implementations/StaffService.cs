using BenLanSystem.Data;
using BenLanSystem.Models.DTOs;
using BenLanSystem.Models.Entities;
using BenLanSystem.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BenLanSystem.Services.Implementations;

public class StaffService(UserManager<Staff> userManager, ApplicationDbContext db) : IStaffService
{
    public async Task<IEnumerable<StaffDto>> GetAllAsync()
    {
        var users = await userManager.Users.OrderBy(u => u.FirstName).ToListAsync();
        var result = new List<StaffDto>();
        foreach (var u in users)
        {
            var roles = await userManager.GetRolesAsync(u);
            result.Add(new StaffDto
            {
                Id = u.Id, Email = u.Email ?? "", FirstName = u.FirstName, LastName = u.LastName,
                Position = u.Position, EmployeeId = u.EmployeeId, HireDate = u.HireDate,
                PhoneNumber = u.PhoneNumber
            });
        }
        return result;
    }

    public async Task<StaffDto?> GetByIdAsync(long id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return null;
        return new StaffDto
        {
            Id = user.Id, Email = user.Email ?? "", FirstName = user.FirstName, LastName = user.LastName,
            Position = user.Position, EmployeeId = user.EmployeeId, HireDate = user.HireDate,
            PhoneNumber = user.PhoneNumber
        };
    }

    public async Task<StaffDto> CreateAsync(StaffCreateDto dto)
    {
        var user = new Staff
        {
            UserName = dto.Email, Email = dto.Email, FirstName = dto.FirstName, LastName = dto.LastName,
            Position = dto.Position, EmployeeId = dto.EmployeeId, PhoneNumber = dto.PhoneNumber,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        return (await GetByIdAsync(user.Id))!;
    }

    public async Task<StaffDto?> UpdateAsync(long id, StaffUpdateDto dto)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return null;
        if (dto.FirstName is not null) user.FirstName = dto.FirstName;
        if (dto.LastName is not null) user.LastName = dto.LastName;
        if (dto.Position is not null) user.Position = dto.Position;
        if (dto.EmployeeId is not null) user.EmployeeId = dto.EmployeeId;
        if (dto.PhoneNumber is not null) user.PhoneNumber = dto.PhoneNumber;
        await userManager.UpdateAsync(user);
        return await GetByIdAsync(id);
    }
}