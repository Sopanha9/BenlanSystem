using System.ComponentModel.DataAnnotations;

namespace BenLanSystem.Models.DTOs;

public class TripDto
{
    public long Id { get; set; }
    public int RouteId { get; set; }
    public int VehicleId { get; set; }
    public DateTime DepartureTimeUtc { get; set; }
    public DateTime? ArrivalTimeUtc { get; set; }
    public decimal BasePrice { get; set; }
    public int AvailableSeats { get; set; }
    public string StatusName { get; set; } = "Open";
    public string? OriginName { get; set; }
    public string? DestinationName { get; set; }
    public string? VehiclePlateNumber { get; set; }
    public string? VehicleBrand { get; set; }
    public string? VehicleModel { get; set; }
}

public class TripCreateDto
{
    [Required]
    public int RouteId { get; set; }

    [Required]
    public int VehicleId { get; set; }

    [Required]
    public DateTime DepartureTimeUtc { get; set; }

    public DateTime? ArrivalTimeUtc { get; set; }

    [Required, Range(0, double.MaxValue)]
    public decimal BasePrice { get; set; }

    [Required, Range(0, int.MaxValue)]
    public int AvailableSeats { get; set; }

    [StringLength(20)]
    public string StatusName { get; set; } = "Open";
}

public class TripUpdateDto
{
    public int? RouteId { get; set; }
    public int? VehicleId { get; set; }
    public DateTime? DepartureTimeUtc { get; set; }
    public DateTime? ArrivalTimeUtc { get; set; }
    [Range(0, double.MaxValue)]
    public decimal? BasePrice { get; set; }
    [Range(0, int.MaxValue)]
    public int? AvailableSeats { get; set; }
    [StringLength(20)]
    public string? StatusName { get; set; }
}