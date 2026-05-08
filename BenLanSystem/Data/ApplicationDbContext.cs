using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BenLanSystem.Models.Entities;
using RouteModel = BenLanSystem.Models.Entities.Route;

namespace BenLanSystem.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<Staff, IdentityRole<long>, long>(options)
{
    public DbSet<Location> Locations { get; set; }
    public DbSet<RouteModel> Routes { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<VehicleHistory> VehicleHistories { get; set; }
    public DbSet<Trip> Trips { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<BookingPassenger> BookingPassengers { get; set; }
    public DbSet<BookingHistory> BookingHistories { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentHistory> PaymentHistories { get; set; }
    public DbSet<BlogPost> BlogPosts { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── Location ──
        builder.Entity<Location>(e =>
        {
            e.ToTable("Locations");
            e.HasKey(l => l.Id);
            e.Property(l => l.Id).HasColumnName("LocationId");
            e.Property(l => l.IsActive).HasDefaultValue(true);
            e.HasIndex(l => l.Name).IsUnique();
        });

        // ── Route ──
        builder.Entity<RouteModel>(e =>
        {
            e.ToTable("Routes");
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasColumnName("RouteId");
            e.Property(r => r.StartLocationId).HasColumnName("StartLocationId");
            e.Property(r => r.EndLocationId).HasColumnName("EndLocationId");
            e.Property(r => r.DistanceKm).HasColumnType("decimal(8,2)");
            e.Property(r => r.IsActive).HasDefaultValue(true);
            e.HasIndex(r => new { r.StartLocationId, r.EndLocationId }).IsUnique();
            e.HasCheckConstraint("CK_Routes_DifferentLocations", "[StartLocationId] <> [EndLocationId]");
        });

        builder.Entity<RouteModel>()
            .HasOne(r => r.StartLocation)
            .WithMany(l => l.OriginRoutes)
            .HasForeignKey(r => r.StartLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RouteModel>()
            .HasOne(r => r.EndLocation)
            .WithMany(l => l.DestinationRoutes)
            .HasForeignKey(r => r.EndLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Vehicle ──
        builder.Entity<Vehicle>(e =>
        {
            e.ToTable("Vehicles");
            e.HasKey(v => v.Id);
            e.Property(v => v.Id).HasColumnName("VehicleId");
            e.Property(v => v.StatusName).HasDefaultValue("Active");
            e.Property(v => v.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(v => v.PlateNumber).IsUnique();
            e.HasCheckConstraint("CK_Vehicles_SeatCapacity", "[SeatCapacity] > 0");
            e.HasCheckConstraint("CK_Vehicles_Transmission", "[Transmission] IS NULL OR [Transmission] IN ('Auto', 'Manual')");
            e.HasCheckConstraint("CK_Vehicles_FuelType", "[FuelType] IS NULL OR [FuelType] IN ('Gas', 'EV', 'Hybrid')");
            e.HasCheckConstraint("CK_Vehicles_StatusName", "[StatusName] IN ('Active', 'Maintenance', 'Retired')");
        });

        // ── VehicleHistory ──
        builder.Entity<VehicleHistory>(e =>
        {
            e.ToTable("VehicleHistories");
            e.HasKey(vh => vh.Id);
            e.Property(vh => vh.Id).HasColumnName("VehicleHistoryId");
        });

        builder.Entity<VehicleHistory>()
            .HasOne(vh => vh.Vehicle)
            .WithMany(v => v.VehicleHistories)
            .HasForeignKey(vh => vh.VehicleId);

        // ── Trip ──
        builder.Entity<Trip>(e =>
        {
            e.ToTable("Trips");
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).HasColumnName("TripId");
            e.Property(t => t.BasePrice).HasColumnType("decimal(10,2)");
            e.Property(t => t.StatusName).HasDefaultValue("Open");
            e.Property(t => t.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(t => new { t.RouteId, t.DepartureTimeUtc });
            e.HasCheckConstraint("CK_Trips_BasePrice", "[BasePrice] >= 0");
            e.HasCheckConstraint("CK_Trips_AvailableSeats", "[AvailableSeats] >= 0");
            e.HasCheckConstraint("CK_Trips_StatusName", "[StatusName] IN ('Open', 'Closed', 'Cancelled', 'Completed')");
        });

        builder.Entity<Trip>()
            .HasOne(t => t.Route)
            .WithMany(r => r.Trips) // RouteModel.Trips
            .HasForeignKey(t => t.RouteId);

        builder.Entity<Trip>()
            .HasOne(t => t.Vehicle)
            .WithMany(v => v.Trips)
            .HasForeignKey(t => t.VehicleId);

        // ── Booking ──
        builder.Entity<Booking>(e =>
        {
            e.ToTable("Bookings");
            e.HasKey(b => b.Id);
            e.Property(b => b.Id).HasColumnName("BookingId");
            e.Property(b => b.UnitPrice).HasColumnType("decimal(10,2)");
            e.Property(b => b.TotalAmount).HasColumnType("decimal(10,2)").HasComputedColumnSql("[SeatsBooked] * [UnitPrice]", stored: true);
            e.Property(b => b.BookingStatus).HasDefaultValue("Pending");
            e.Property(b => b.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(b => b.TripId);
            e.HasIndex(b => b.CustomerId);
            e.HasCheckConstraint("CK_Bookings_SeatsBooked", "[SeatsBooked] > 0");
            e.HasCheckConstraint("CK_Bookings_UnitPrice", "[UnitPrice] >= 0");
            e.HasCheckConstraint("CK_Bookings_BookingStatus", "[BookingStatus] IN ('Pending', 'Confirmed', 'Cancelled', 'Completed')");
        });

        builder.Entity<Booking>()
            .HasOne(b => b.Trip)
            .WithMany(t => t.Bookings)
            .HasForeignKey(b => b.TripId);

        builder.Entity<Booking>()
            .HasOne(b => b.Customer)
            .WithMany(c => c.CustomerBookings)
            .HasForeignKey(b => b.CustomerId);

        // ── BookingPassenger ──
        builder.Entity<BookingPassenger>(e =>
        {
            e.ToTable("BookingPassengers");
            e.HasKey(bp => bp.Id);
            e.Property(bp => bp.Id).HasColumnName("BookingPassengerId");
            e.HasIndex(bp => new { bp.BookingId, bp.SeatNumber }).IsUnique();
        });

        builder.Entity<BookingPassenger>()
            .HasOne(bp => bp.Booking)
            .WithMany(b => b.BookingPassengers)
            .HasForeignKey(bp => bp.BookingId);

        // ── BookingHistory ──
        builder.Entity<BookingHistory>(e =>
        {
            e.ToTable("BookingHistories");
            e.HasKey(bh => bh.Id);
            e.Property(bh => bh.Id).HasColumnName("BookingHistoryId");
        });

        builder.Entity<BookingHistory>()
            .HasOne(bh => bh.Booking)
            .WithMany(b => b.BookingHistories)
            .HasForeignKey(bh => bh.BookingId);

        // ── Payment ──
        builder.Entity<Payment>(e =>
        {
            e.ToTable("Payments");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("PaymentId");
            e.Property(p => p.Amount).HasColumnType("decimal(10,2)");
            e.Property(p => p.PaymentStatus).HasDefaultValue("Pending");
            e.Property(p => p.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(p => p.BookingId);
            e.HasCheckConstraint("CK_Payments_Amount", "[Amount] >= 0");
            e.HasCheckConstraint("CK_Payments_PaymentMethod", "[PaymentMethod] IN ('ABA', 'ACLEDA', 'Wing', 'Cash', 'Card')");
            e.HasCheckConstraint("CK_Payments_PaymentStatus", "[PaymentStatus] IN ('Pending', 'Paid', 'Failed', 'Refunded')");
        });

        builder.Entity<Payment>()
            .HasOne(p => p.Booking)
            .WithMany(b => b.Payments)
            .HasForeignKey(p => p.BookingId);

        // ── PaymentHistory ──
        builder.Entity<PaymentHistory>(e =>
        {
            e.ToTable("PaymentHistories");
            e.HasKey(ph => ph.Id);
            e.Property(ph => ph.Id).HasColumnName("PaymentHistoryId");
        });

        builder.Entity<PaymentHistory>()
            .HasOne(ph => ph.Payment)
            .WithMany(p => p.PaymentHistories)
            .HasForeignKey(ph => ph.PaymentId);

        // ── BlogPost ──
        builder.Entity<BlogPost>(e =>
        {
            e.ToTable("BlogPosts");
            e.HasKey(b => b.Id);
            e.Property(b => b.Id).HasColumnName("BlogPostId");
            e.Property(b => b.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(b => b.AuthorId);
        });

        builder.Entity<BlogPost>()
            .HasOne(b => b.Author)
            .WithMany()
            .HasForeignKey(b => b.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}