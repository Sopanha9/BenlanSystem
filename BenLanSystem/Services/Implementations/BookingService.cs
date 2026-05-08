using BenLanSystem.Data;
using BenLanSystem.Models.DTOs;
using BenLanSystem.Models.Entities;
using BenLanSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BenLanSystem.Services.Implementations;

public class BookingService(ApplicationDbContext db) : IBookingService
{
    public async Task<PagedResultDto<BookingDto>> GetByCustomerAsync(long customerId, int page = 1, int pageSize = 10)
    {
        var query = db.Bookings.Include(b => b.Trip).ThenInclude(t => t.Route).ThenInclude(r => r.StartLocation)
            .Include(b => b.Trip).ThenInclude(t => t.Route).ThenInclude(r => r.EndLocation)
            .Include(b => b.BookingPassengers)
            .Where(b => b.CustomerId == customerId)
            .OrderByDescending(b => b.CreatedAtUtc);

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(b => new BookingDto
        {
            Id = b.Id, TripId = b.TripId, CustomerId = b.CustomerId, SeatsBooked = b.SeatsBooked,
            UnitPrice = b.UnitPrice, TotalAmount = b.TotalAmount, BookingStatus = b.BookingStatus,
            Notes = b.Notes, CreatedAtUtc = b.CreatedAtUtc,
            OriginName = b.Trip.Route.StartLocation.Name, DestinationName = b.Trip.Route.EndLocation.Name,
            DepartureTimeUtc = b.Trip.DepartureTimeUtc,
            Passengers = b.BookingPassengers.Select(bp => new BookingPassengerDto
            {
                Id = bp.Id, BookingId = bp.BookingId, PassengerName = bp.PassengerName, SeatNumber = bp.SeatNumber
            }).ToList()
        }).ToListAsync();

        return new PagedResultDto<BookingDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
    }

    public async Task<BookingDto?> GetByIdAsync(long id)
    {
        var b = await db.Bookings.Include(b => b.Trip).ThenInclude(t => t.Route).ThenInclude(r => r.StartLocation)
            .Include(b => b.Trip).ThenInclude(t => t.Route).ThenInclude(r => r.EndLocation)
            .Include(b => b.BookingPassengers)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (b is null) return null;
        return new BookingDto
        {
            Id = b.Id, TripId = b.TripId, CustomerId = b.CustomerId, SeatsBooked = b.SeatsBooked,
            UnitPrice = b.UnitPrice, TotalAmount = b.TotalAmount, BookingStatus = b.BookingStatus,
            Notes = b.Notes, CreatedAtUtc = b.CreatedAtUtc,
            OriginName = b.Trip.Route.StartLocation.Name, DestinationName = b.Trip.Route.EndLocation.Name,
            DepartureTimeUtc = b.Trip.DepartureTimeUtc,
            Passengers = b.BookingPassengers.Select(bp => new BookingPassengerDto
            {
                Id = bp.Id, BookingId = bp.BookingId, PassengerName = bp.PassengerName, SeatNumber = bp.SeatNumber
            }).ToList()
        };
    }

    public async Task<BookingDto> CreateAsync(long customerId, BookingCreateDto dto)
    {
        var booking = new Booking
        {
            TripId = dto.TripId, CustomerId = customerId, SeatsBooked = dto.SeatsBooked,
            UnitPrice = dto.UnitPrice, BookingStatus = "Pending", Notes = dto.Notes
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        foreach (var p in dto.Passengers)
        {
            db.BookingPassengers.Add(new BookingPassenger
            {
                BookingId = booking.Id, PassengerName = p.PassengerName, SeatNumber = p.SeatNumber
            });
        }

        db.BookingHistories.Add(new BookingHistory
        {
            BookingId = booking.Id, ChangeDate = DateTime.UtcNow, ChangedBy = customerId.ToString(),
            OldStatus = "None", NewStatus = "Pending", Remarks = "Booking created"
        });

        await db.SaveChangesAsync();

        // Decrease available seats
        var trip = await db.Trips.FindAsync(dto.TripId);
        if (trip is not null)
        {
            trip.AvailableSeats -= dto.SeatsBooked;
            trip.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        return (await GetByIdAsync(booking.Id))!;
    }

    public async Task<BookingDto?> CancelAsync(long id, long customerId, BookingCancelDto? cancelDto)
    {
        var booking = await db.Bookings.Include(b => b.Trip).FirstOrDefaultAsync(b => b.Id == id);
        if (booking is null || booking.CustomerId != customerId) return null;
        if (booking.BookingStatus is not ("Pending" or "Confirmed")) return null;

        var oldStatus = booking.BookingStatus;
        booking.BookingStatus = "Cancelled";
        booking.UpdatedAtUtc = DateTime.UtcNow;

        db.BookingHistories.Add(new BookingHistory
        {
            BookingId = booking.Id, ChangeDate = DateTime.UtcNow, ChangedBy = customerId.ToString(),
            OldStatus = oldStatus, NewStatus = "Cancelled", Remarks = cancelDto?.Reason ?? "Cancelled by customer"
        });

        // Restore available seats
        var trip = await db.Trips.FindAsync(booking.TripId);
        if (trip is not null)
        {
            trip.AvailableSeats += booking.SeatsBooked;
            trip.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<IEnumerable<BookingDto>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        return await db.Bookings.Include(b => b.Trip).ThenInclude(t => t.Route).ThenInclude(r => r.StartLocation)
            .Include(b => b.Trip).ThenInclude(t => t.Route).ThenInclude(r => r.EndLocation)
            .Include(b => b.BookingPassengers)
            .OrderByDescending(b => b.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(b => new BookingDto
            {
                Id = b.Id, TripId = b.TripId, CustomerId = b.CustomerId, SeatsBooked = b.SeatsBooked,
                UnitPrice = b.UnitPrice, TotalAmount = b.TotalAmount, BookingStatus = b.BookingStatus,
                Notes = b.Notes, CreatedAtUtc = b.CreatedAtUtc,
                OriginName = b.Trip.Route.StartLocation.Name, DestinationName = b.Trip.Route.EndLocation.Name,
                DepartureTimeUtc = b.Trip.DepartureTimeUtc,
                Passengers = b.BookingPassengers.Select(bp => new BookingPassengerDto
                {
                    Id = bp.Id, BookingId = bp.BookingId, PassengerName = bp.PassengerName, SeatNumber = bp.SeatNumber
                }).ToList()
            }).ToListAsync();
    }
}