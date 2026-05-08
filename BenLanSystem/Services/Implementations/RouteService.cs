using BenLanSystem.Data;
using BenLanSystem.Models.DTOs;
using BenLanSystem.Models.Entities;
using BenLanSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using RouteModel = BenLanSystem.Models.Entities.Route;

namespace BenLanSystem.Services.Implementations;

public class RouteService(ApplicationDbContext db) : IRouteService
{
    public async Task<IEnumerable<RouteDto>> GetAllAsync()
        => await db.Routes
            .Include(r => r.StartLocation).Include(r => r.EndLocation)
            .Where(r => r.IsActive)
            .Select(r => new RouteDto
            {
                Id = r.Id, StartLocationId = r.StartLocationId, EndLocationId = r.EndLocationId,
                DistanceKm = r.DistanceKm, EstimatedMinutes = r.EstimatedMinutes, IsActive = r.IsActive,
                OriginName = r.StartLocation.Name, DestinationName = r.EndLocation.Name
            })
            .ToListAsync();

    public async Task<RouteDto?> GetByIdAsync(int id)
    {
        var r = await db.Routes.Include(r => r.StartLocation).Include(r => r.EndLocation).FirstOrDefaultAsync(r => r.Id == id);
        if (r is null) return null;
        return new RouteDto { Id = r.Id, StartLocationId = r.StartLocationId, EndLocationId = r.EndLocationId, DistanceKm = r.DistanceKm, EstimatedMinutes = r.EstimatedMinutes, IsActive = r.IsActive, OriginName = r.StartLocation.Name, DestinationName = r.EndLocation.Name };
    }

    public async Task<RouteDto> CreateAsync(RouteCreateDto dto)
    {
        var route = new RouteModel { StartLocationId = dto.StartLocationId, EndLocationId = dto.EndLocationId, DistanceKm = dto.DistanceKm, EstimatedMinutes = dto.EstimatedMinutes, IsActive = dto.IsActive };
        db.Routes.Add(route);
        await db.SaveChangesAsync();
        return (await GetByIdAsync(route.Id))!;
    }

    public async Task<RouteDto?> UpdateAsync(int id, RouteUpdateDto dto)
    {
        var route = await db.Routes.FindAsync(id);
        if (route is null) return null;
        if (dto.DistanceKm.HasValue) route.DistanceKm = dto.DistanceKm;
        if (dto.EstimatedMinutes.HasValue) route.EstimatedMinutes = dto.EstimatedMinutes;
        route.IsActive = dto.IsActive;
        await db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var route = await db.Routes.FindAsync(id);
        if (route is null) return false;
        route.IsActive = false;
        await db.SaveChangesAsync();
        return true;
    }
}