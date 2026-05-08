using System.ComponentModel.DataAnnotations;

namespace BenLanSystem.Models.Entities;

public class Booking
{
    public long Id { get; set; }

    [Required]
    public long TripId { get; set; }

    [Required]
    public long CustomerId { get; set; }

    [Required]
    public int SeatsBooked { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    public decimal TotalAmount { get; set; } // Computed: SeatsBooked * UnitPrice

    [Required]
    [StringLength(20)]
    public string BookingStatus { get; set; } = "Pending";

    [StringLength(300)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    // Navigation properties
    public Trip Trip { get; set; } = null!;
    public Staff Customer { get; set; } = null!;
    public ICollection<BookingPassenger> BookingPassengers { get; set; } = new List<BookingPassenger>();
    public ICollection<BookingHistory> BookingHistories { get; set; } = new List<BookingHistory>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}