using System.ComponentModel.DataAnnotations;

namespace BenLanSystem.Models.DTOs;

public class BookingPassengerDto
{
    public long Id { get; set; }
    public long BookingId { get; set; }
    public string PassengerName { get; set; } = string.Empty;
    public string SeatNumber { get; set; } = string.Empty;
}

public class BookingPassengerCreateDto
{
    [Required, StringLength(120)]
    public string PassengerName { get; set; } = string.Empty;

    [Required, StringLength(10)]
    public string SeatNumber { get; set; } = string.Empty;
}