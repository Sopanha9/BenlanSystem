using System.ComponentModel.DataAnnotations;

namespace BenLanSystem.Models.Entities;

public class Route
{
    public int Id { get; set; }

    [Required]
    public int StartLocationId { get; set; }

    [Required]
    public int EndLocationId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? DistanceKm { get; set; }

    public int? EstimatedMinutes { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Location StartLocation { get; set; } = null!;
    public Location EndLocation { get; set; } = null!;
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}