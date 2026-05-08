using System.ComponentModel.DataAnnotations;

namespace BenLanSystem.Models.DTOs;

public class BookingDto
{
    public long Id { get; set; }
    public long TripId { get; set; }
    public long CustomerId { get; set; }
    public int SeatsBooked { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public string BookingStatus { get; set; } = "Pending";
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? OriginName { get; set; }
    public string? DestinationName { get; set; }
    public DateTime DepartureTimeUtc { get; set; }
    public List<BookingPassengerDto> Passengers { get; set; } = [];
}

public class BookingCreateDto
{
    [Required]
    public long TripId { get; set; }

    [Required, Range(1, 100)]
    public int SeatsBooked { get; set; }

    [Required, Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [StringLength(300)]
    public string? Notes { get; set; }

    [Required]
    public List<BookingPassengerCreateDto> Passengers { get; set; } = [];
}

public class BookingCancelDto
{
    [StringLength(300)]
    public string? Reason { get; set; }
}