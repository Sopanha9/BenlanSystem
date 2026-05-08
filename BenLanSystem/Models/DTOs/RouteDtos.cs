using System.ComponentModel.DataAnnotations;

namespace BenLanSystem.Models.DTOs;

public class RouteDto
{
    public int Id { get; set; }
    public int StartLocationId { get; set; }
    public int EndLocationId { get; set; }
    public decimal? DistanceKm { get; set; }
    public int? EstimatedMinutes { get; set; }
    public bool IsActive { get; set; }
    public string? OriginName { get; set; }
    public string? DestinationName { get; set; }
}

public class RouteCreateDto
{
    [Required]
    public int StartLocationId { get; set; }

    [Required]
    public int EndLocationId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? DistanceKm { get; set; }

    public int? EstimatedMinutes { get; set; }

    public bool IsActive { get; set; } = true;
}

public class RouteUpdateDto
{
    [Range(0, double.MaxValue)]
    public decimal? DistanceKm { get; set; }

    public int? EstimatedMinutes { get; set; }

    public bool IsActive { get; set; } = true;
}