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
        await using var tx = await db.Database.BeginTransactionAsync();

        try
        {
            var trip = await db.Trips.FindAsync(dto.TripId)
                ?? throw new InvalidOperationException("Trip not found");

            if (trip.StatusName != "Open")
                throw new InvalidOperationException("Trip is not open for booking");

            if (trip.AvailableSeats < dto.SeatsBooked)
                throw new InvalidOperationException($"Only {trip.AvailableSeats} seats available");

            if (dto.Passengers.Count != dto.SeatsBooked)
                throw new InvalidOperationException("Passenger count must match seat count");

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

            await db.Entry(trip).ReloadAsync();
            if (trip.StatusName != "Open")
                throw new InvalidOperationException("Trip is no longer open for booking");
            if (trip.AvailableSeats < dto.SeatsBooked)
                throw new InvalidOperationException($"Seats no longer available. Only {trip.AvailableSeats} left.");

            trip.AvailableSeats -= dto.SeatsBooked;
            trip.UpdatedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            return (await GetByIdAsync(booking.Id))!;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("Seats changed while booking. Please search again and retry.", ex);
        }
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

    public async Task<BookingDto?> UpdateStatusAsync(long id, long changedByUserId, BookingStatusUpdateDto dto)
    {
        var booking = await db.Bookings.FindAsync(id);
        if (booking is null) return null;

        var allowedStatuses = new[] { "Pending", "Confirmed", "Cancelled", "Completed" };
        if (!allowedStatuses.Contains(dto.StatusName))
            throw new InvalidOperationException("Invalid booking status.");

        var oldStatus = booking.BookingStatus;
        if (oldStatus == dto.StatusName) return await GetByIdAsync(id);
        if (oldStatus is "Cancelled" or "Completed")
            throw new InvalidOperationException($"Cannot change a {oldStatus.ToLowerInvariant()} booking.");
        if (oldStatus == "Pending" && dto.StatusName == "Completed")
            throw new InvalidOperationException("Confirm the booking before completing it.");

        booking.BookingStatus = dto.StatusName;
        booking.UpdatedAtUtc = DateTime.UtcNow;

        db.BookingHistories.Add(new BookingHistory
        {
            BookingId = booking.Id,
            ChangeDate = DateTime.UtcNow,
            ChangedBy = changedByUserId.ToString(),
            OldStatus = oldStatus,
            NewStatus = dto.StatusName,
            Remarks = dto.Remarks ?? "Status changed by staff"
        });

        if (dto.StatusName == "Cancelled" && oldStatus != "Cancelled")
        {
            var trip = await db.Trips.FindAsync(booking.TripId);
            if (trip is not null)
            {
                trip.AvailableSeats += booking.SeatsBooked;
                trip.UpdatedAtUtc = DateTime.UtcNow;
            }
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
