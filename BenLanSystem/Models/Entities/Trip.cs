using System.ComponentModel.DataAnnotations;

namespace BenLanSystem.Models.Entities;

public class Trip
{
    public long Id { get; set; }

    [Required]
    public int RouteId { get; set; }

    [Required]
    public int VehicleId { get; set; }

    [Required]
    public DateTime DepartureTimeUtc { get; set; }

    public DateTime? ArrivalTimeUtc { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal BasePrice { get; set; }

    [Required]
    public int AvailableSeats { get; set; }

    [Required]
    [StringLength(20)]
    public string StatusName { get; set; } = "Open";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    // Navigation properties
    public Route Route { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}