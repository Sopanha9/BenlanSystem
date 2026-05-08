using BenLanSystem.Data;
using BenLanSystem.Models.DTOs;
using BenLanSystem.Models.Entities;
using BenLanSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BenLanSystem.Services.Implementations;

public class LocationService(ApplicationDbContext db) : ILocationService
{
    public async Task<IEnumerable<LocationDto>> GetAllAsync()
        => await db.Locations.Where(l => l.IsActive).OrderBy(l => l.Name)
            .Select(l => new LocationDto { Id = l.Id, Name = l.Name, Province = l.Province, AddressLine = l.AddressLine, IsActive = l.IsActive })
            .ToListAsync();

    public async Task<LocationDto?> GetByIdAsync(int id)
        => await db.Locations.FindAsync(id) is { } loc
            ? new LocationDto { Id = loc.Id, Name = loc.Name, Province = loc.Province, AddressLine = loc.AddressLine, IsActive = loc.IsActive }
            : null;

    public async Task<LocationDto> CreateAsync(LocationCreateDto dto)
    {
        var loc = new Location { Name = dto.Name, Province = dto.Province, AddressLine = dto.AddressLine, IsActive = dto.IsActive };
        db.Locations.Add(loc);
        await db.SaveChangesAsync();
        return new LocationDto { Id = loc.Id, Name = loc.Name, Province = loc.Province, AddressLine = loc.AddressLine, IsActive = loc.IsActive };
    }

    public async Task<LocationDto?> UpdateAsync(int id, LocationUpdateDto dto)
    {
        var loc = await db.Locations.FindAsync(id);
        if (loc is null) return null;
        loc.Name = dto.Name;
        loc.Province = dto.Province;
        loc.AddressLine = dto.AddressLine;
        loc.IsActive = dto.IsActive;
        await db.SaveChangesAsync();
        return new LocationDto { Id = loc.Id, Name = loc.Name, Province = loc.Province, AddressLine = loc.AddressLine, IsActive = loc.IsActive };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var loc = await db.Locations.FindAsync(id);
        if (loc is null) return false;
        loc.IsActive = false;
        await db.SaveChangesAsync();
        return true;
    }
}