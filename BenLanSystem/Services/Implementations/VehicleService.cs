using BenLanSystem.Data;
using BenLanSystem.Models.DTOs;
using BenLanSystem.Models.Entities;
using BenLanSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BenLanSystem.Services.Implementations;

public class VehicleService(ApplicationDbContext db) : IVehicleService
{
    public async Task<IEnumerable<VehicleDto>> GetAllAsync()
        => await db.Vehicles.OrderBy(v => v.PlateNumber).Select(v => new VehicleDto
        {
            Id = v.Id, PlateNumber = v.PlateNumber, Brand = v.Brand, Model = v.Model,
            SeatCapacity = v.SeatCapacity, Transmission = v.Transmission, FuelType = v.FuelType,
            StatusName = v.StatusName, ImageUrl = v.ImageUrl
        }).ToListAsync();

    public async Task<VehicleDto?> GetByIdAsync(int id)
    {
        var v = await db.Vehicles.FindAsync(id);
        if (v is null) return null;
        return new VehicleDto { Id = v.Id, PlateNumber = v.PlateNumber, Brand = v.Brand, Model = v.Model, SeatCapacity = v.SeatCapacity, Transmission = v.Transmission, FuelType = v.FuelType, StatusName = v.StatusName, ImageUrl = v.ImageUrl };
    }

    public async Task<VehicleDto> CreateAsync(VehicleCreateDto dto)
    {
        var vehicle = new Vehicle
        {
            PlateNumber = dto.PlateNumber, Brand = dto.Brand, Model = dto.Model,
            SeatCapacity = dto.SeatCapacity, Transmission = dto.Transmission, FuelType = dto.FuelType,
            StatusName = dto.StatusName, ImageUrl = dto.ImageUrl
        };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        return (await GetByIdAsync(vehicle.Id))!;
    }

    public async Task<VehicleDto?> UpdateAsync(int id, VehicleUpdateDto dto)
    {
        var vehicle = await db.Vehicles.FindAsync(id);
        if (vehicle is null) return null;
        if (dto.PlateNumber is not null) vehicle.PlateNumber = dto.PlateNumber;
        if (dto.Brand is not null) vehicle.Brand = dto.Brand;
        if (dto.Model is not null) vehicle.Model = dto.Model;
        if (dto.SeatCapacity.HasValue) vehicle.SeatCapacity = dto.SeatCapacity.Value;
        if (dto.Transmission is not null) vehicle.Transmission = dto.Transmission;
        if (dto.FuelType is not null) vehicle.FuelType = dto.FuelType;
        if (dto.StatusName is not null) vehicle.StatusName = dto.StatusName;
        if (dto.ImageUrl is not null) vehicle.ImageUrl = dto.ImageUrl;
        vehicle.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var vehicle = await db.Vehicles.FindAsync(id);
        if (vehicle is null) return false;
        vehicle.StatusName = "Retired";
        vehicle.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }
}