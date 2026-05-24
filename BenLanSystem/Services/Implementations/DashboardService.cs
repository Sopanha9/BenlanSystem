using BenLanSystem.Data;
using BenLanSystem.Models.DTOs;
using BenLanSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BenLanSystem.Services.Implementations;

public class DashboardService(ApplicationDbContext db) : IDashboardService
{
    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {
        var today = DateTime.UtcNow.Date;
        var firstOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var totalBookings = await db.Bookings.CountAsync();
        var bookingsThisMonth = await db.Bookings.CountAsync(b => b.CreatedAtUtc >= firstOfMonth);
        var activeFleet = await db.Vehicles.CountAsync(v => v.StatusName == "Active");
        var fleetInMaintenance = await db.Vehicles.CountAsync(v => v.StatusName == "Maintenance");
        var bookingsToday = await db.Bookings.CountAsync(b => b.CreatedAtUtc.Date == today);
        var confirmedBookings = await db.Bookings.CountAsync(b => b.BookingStatus == "Confirmed");
        var pendingBookings = await db.Bookings.CountAsync(b => b.BookingStatus == "Pending");
        var pendingPayments = await db.Payments.CountAsync(p => p.PaymentStatus == "Pending");

        var grossRevenue = await db.Payments
            .Where(p => p.PaymentStatus == "Paid")
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        var confirmedPercent = totalBookings > 0
            ? Math.Round((decimal)confirmedBookings / totalBookings * 100, 0)
            : 0m;

        return new DashboardSummaryDto
        {
            TotalBookings = totalBookings,
            BookingsThisMonth = bookingsThisMonth,
            ActiveFleet = activeFleet,
            FleetInMaintenance = fleetInMaintenance,
            BookingsToday = bookingsToday,
            BookingsConfirmedPercent = confirmedPercent,
            GrossRevenue = grossRevenue,
            PendingBookings = pendingBookings,
            PendingPayments = pendingPayments
        };
    }

    public async Task<IEnumerable<RouteLoadDto>> GetRouteLoadAsync()
    {
        var today = DateTime.UtcNow.Date;

        var routes = await db.Routes
            .Include(r => r.StartLocation)
            .Include(r => r.EndLocation)
            .Where(r => r.IsActive)
            .ToListAsync();

        var result = new List<RouteLoadDto>();

        foreach (var route in routes)
        {
            var trips = await db.Trips
                .Include(t => t.Vehicle)
                .Where(t => t.RouteId == route.Id && t.DepartureTimeUtc.Date >= today)
                .ToListAsync();

            if (trips.Count == 0) continue;

            var tripIds = trips.Select(t => t.Id).ToList();
            var bookings = await db.Bookings
                .Where(b => tripIds.Contains(b.TripId) && b.BookingStatus != "Cancelled")
                .ToListAsync();

            var passengers = bookings.Sum(b => b.SeatsBooked);
            var revenue = bookings.Sum(b => b.TotalAmount);
            var totalCapacity = trips.Sum(t => t.Vehicle?.SeatCapacity ?? 0);
            var loadPercent = totalCapacity > 0
                ? (int)Math.Round((double)passengers / totalCapacity * 100)
                : 0;

            result.Add(new RouteLoadDto
            {
                RouteName = $"{route.StartLocation?.Name} → {route.EndLocation?.Name}",
                Trips = trips.Count,
                Passengers = passengers,
                LoadPercent = loadPercent,
                Revenue = revenue
            });
        }

        return result.OrderByDescending(r => r.Revenue).Take(10);
    }

    public async Task<IEnumerable<DashboardAlertDto>> GetAlertsAsync()
    {
        var alerts = new List<DashboardAlertDto>();

        var today = DateTime.UtcNow.Date;

        // Low seat availability alerts
        var trips = await db.Trips
            .Include(t => t.Vehicle)
            .Include(t => t.Route).ThenInclude(r => r.StartLocation)
            .Include(t => t.Route).ThenInclude(r => r.EndLocation)
            .Where(t => t.StatusName == "Open" && t.DepartureTimeUtc.Date >= today)
            .ToListAsync();

        foreach (var trip in trips)
        {
            var capacity = trip.Vehicle?.SeatCapacity ?? 0;
            if (capacity <= 0) continue;

            var booked = capacity - trip.AvailableSeats;
            var percent = (double)booked / capacity * 100;

            if (percent >= 90)
            {
                alerts.Add(new DashboardAlertDto
                {
                    Type = "Trip Load",
                    Message = $"Trip {trip.Route?.StartLocation?.Name} → {trip.Route?.EndLocation?.Name} on {trip.DepartureTimeUtc:yyyy-MM-dd HH:mm} is {percent:F0}% full",
                    ActionLabel = "View",
                    ActionUrl = $"/Admin/Trips",
                    Severity = "danger"
                });
            }
            else if (percent >= 75)
            {
                alerts.Add(new DashboardAlertDto
                {
                    Type = "Trip Load",
                    Message = $"Trip {trip.Route?.StartLocation?.Name} → {trip.Route?.EndLocation?.Name} on {trip.DepartureTimeUtc:yyyy-MM-dd HH:mm} is {percent:F0}% full",
                    ActionLabel = "View",
                    ActionUrl = $"/Admin/Trips",
                    Severity = "warning"
                });
            }
        }

        // Pending bookings alert
        var pendingBookings = await db.Bookings.CountAsync(b => b.BookingStatus == "Pending");
        if (pendingBookings > 0)
        {
            alerts.Add(new DashboardAlertDto
            {
                Type = "Bookings",
                Message = $"{pendingBookings} booking(s) awaiting confirmation",
                ActionLabel = "Review",
                ActionUrl = "/Admin/Bookings",
                Severity = "warning"
            });
        }

        // Pending payments alert
        var pendingPayments = await db.Payments
            .Where(p => p.PaymentStatus == "Pending")
            .ToListAsync();

        if (pendingPayments.Count > 0)
        {
            var pendingTotal = pendingPayments.Sum(p => p.Amount);
            alerts.Add(new DashboardAlertDto
            {
                Type = "Payments",
                Message = $"{pendingPayments.Count} pending payment(s) totaling ${pendingTotal:N2}",
                ActionLabel = "Process",
                ActionUrl = "/Admin/Payments",
                Severity = pendingPayments.Count > 5 ? "danger" : "warning"
            });
        }

        // Vehicles in maintenance
        var maintenanceVehicles = await db.Vehicles
            .Where(v => v.StatusName == "Maintenance")
            .ToListAsync();

        foreach (var vehicle in maintenanceVehicles)
        {
            alerts.Add(new DashboardAlertDto
            {
                Type = "Fleet",
                Message = $"Vehicle {vehicle.PlateNumber} ({vehicle.Brand} {vehicle.Model}) is in maintenance",
                ActionLabel = "Schedule",
                ActionUrl = "/Admin/Vehicles",
                Severity = "info"
            });
        }

        return alerts.OrderBy(a => a.Severity switch { "danger" => 0, "warning" => 1, _ => 2 }).Take(8);
    }
}
