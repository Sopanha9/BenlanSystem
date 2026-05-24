using BenLanSystem.Data;
using BenLanSystem.Models.DTOs;
using BenLanSystem.Models.Entities;
using BenLanSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BenLanSystem.Services.Implementations;

public class TripService(ApplicationDbContext db) : ITripService
{
    public async Task<PagedResultDto<TripSearchResultDto>> SearchAsync(TripSearchDto search)
    {
        var query = db.Trips
            .Include(t => t.Route).ThenInclude(r => r.StartLocation)
            .Include(t => t.Route).ThenInclude(r => r.EndLocation)
            .Include(t => t.Vehicle)
            .AsQueryable();

        if (string.IsNullOrWhiteSpace(search.StatusName))
        {
            query = query.Where(t => t.StatusName == "Open");
        }
        else if (!search.StatusName.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(t => t.StatusName == search.StatusName);
        }

        if (search.OriginId.HasValue) query = query.Where(t => t.Route.StartLocationId == search.OriginId);
        if (search.DestinationId.HasValue) query = query.Where(t => t.Route.EndLocationId == search.DestinationId);
        if (search.DepartureDate.HasValue)
        {
            var date = search.DepartureDate.Value.Date;
            var nextDay = date.AddDays(1);
            query = query.Where(t => t.DepartureTimeUtc >= date && t.DepartureTimeUtc < nextDay);
        }
        if (search.MinPrice.HasValue) query = query.Where(t => t.BasePrice >= search.MinPrice);
        if (search.MaxPrice.HasValue) query = query.Where(t => t.BasePrice <= search.MaxPrice);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(t => t.DepartureTimeUtc)
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize)
            .Select(t => new TripSearchResultDto
            {
                Id = t.Id, RouteId = t.RouteId,
                OriginName = t.Route.StartLocation.Name, DestinationName = t.Route.EndLocation.Name,
                DepartureTimeUtc = t.DepartureTimeUtc, ArrivalTimeUtc = t.ArrivalTimeUtc,
                BasePrice = t.BasePrice, AvailableSeats = t.AvailableSeats, StatusName = t.StatusName,
                VehicleInfo = t.Vehicle.Brand != null ? $"{t.Vehicle.Brand} {t.Vehicle.Model}" : t.Vehicle.PlateNumber,
                VehicleSeatCapacity = t.Vehicle.SeatCapacity,
                EstimatedMinutes = t.Route.EstimatedMinutes, DistanceKm = t.Route.DistanceKm
            })
            .ToListAsync();

        return new PagedResultDto<TripSearchResultDto> { Items = items, TotalCount = totalCount, Page = search.Page, PageSize = search.PageSize };
    }

    public async Task<TripDto?> GetByIdAsync(long id)
    {
        var t = await db.Trips.Include(t => t.Route).ThenInclude(r => r.StartLocation)
            .Include(t => t.Route).ThenInclude(r => r.EndLocation).Include(t => t.Vehicle)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (t is null) return null;
        return new TripDto
        {
            Id = t.Id, RouteId = t.RouteId, VehicleId = t.VehicleId,
            DepartureTimeUtc = t.DepartureTimeUtc, ArrivalTimeUtc = t.ArrivalTimeUtc,
            BasePrice = t.BasePrice, AvailableSeats = t.AvailableSeats, StatusName = t.StatusName,
            OriginName = t.Route.StartLocation.Name, DestinationName = t.Route.EndLocation.Name,
            VehiclePlateNumber = t.Vehicle.PlateNumber, VehicleBrand = t.Vehicle.Brand, VehicleModel = t.Vehicle.Model,
            VehicleSeatCapacity = t.Vehicle.SeatCapacity
        };
    }

    public async Task<TripDto> CreateAsync(TripCreateDto dto)
    {
        var trip = new Trip
        {
            RouteId = dto.RouteId, VehicleId = dto.VehicleId,
            DepartureTimeUtc = dto.DepartureTimeUtc, ArrivalTimeUtc = dto.ArrivalTimeUtc,
            BasePrice = dto.BasePrice, AvailableSeats = dto.AvailableSeats, StatusName = dto.StatusName
        };
        db.Trips.Add(trip);
        await db.SaveChangesAsync();
        return (await GetByIdAsync(trip.Id))!;
    }

    public async Task<TripDto?> UpdateAsync(long id, TripUpdateDto dto)
    {
        var trip = await db.Trips.FindAsync(id);
        if (trip is null) return null;
        if (dto.RouteId.HasValue) trip.RouteId = dto.RouteId.Value;
        if (dto.VehicleId.HasValue) trip.VehicleId = dto.VehicleId.Value;
        if (dto.DepartureTimeUtc.HasValue) trip.DepartureTimeUtc = dto.DepartureTimeUtc.Value;
        if (dto.ArrivalTimeUtc.HasValue) trip.ArrivalTimeUtc = dto.ArrivalTimeUtc;
        if (dto.BasePrice.HasValue) trip.BasePrice = dto.BasePrice.Value;
        if (dto.AvailableSeats.HasValue) trip.AvailableSeats = dto.AvailableSeats.Value;
        if (dto.StatusName is not null) trip.StatusName = dto.StatusName;
        trip.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var trip = await db.Trips.FindAsync(id);
        if (trip is null) return false;
        trip.StatusName = "Cancelled";
        trip.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<TripDto>> GetByRouteAsync(int routeId)
        => await db.Trips.Include(t => t.Route).ThenInclude(r => r.StartLocation)
            .Include(t => t.Route).ThenInclude(r => r.EndLocation).Include(t => t.Vehicle)
            .Where(t => t.RouteId == routeId && t.StatusName == "Open")
            .Select(t => new TripDto
            {
                Id = t.Id, RouteId = t.RouteId, VehicleId = t.VehicleId,
                DepartureTimeUtc = t.DepartureTimeUtc, ArrivalTimeUtc = t.ArrivalTimeUtc,
                BasePrice = t.BasePrice, AvailableSeats = t.AvailableSeats, StatusName = t.StatusName,
                OriginName = t.Route.StartLocation.Name, DestinationName = t.Route.EndLocation.Name,
                VehiclePlateNumber = t.Vehicle.PlateNumber, VehicleBrand = t.Vehicle.Brand, VehicleModel = t.Vehicle.Model
            })
            .ToListAsync();
}
