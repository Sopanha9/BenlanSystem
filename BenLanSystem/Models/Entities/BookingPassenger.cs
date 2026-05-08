using System.ComponentModel.DataAnnotations;

namespace BenLanSystem.Models.Entities;

public class BookingPassenger
{
    public long Id { get; set; }

    [Required]
    public long BookingId { get; set; }

    [Required]
    [StringLength(120)]
    public string PassengerName { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string SeatNumber { get; set; } = string.Empty;

    // Navigation properties
    public Booking Booking { get; set; } = null!;
}