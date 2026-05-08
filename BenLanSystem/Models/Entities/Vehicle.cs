using System.ComponentModel.DataAnnotations;

namespace BenLanSystem.Models.Entities;

public class Vehicle
{
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string PlateNumber { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Brand { get; set; }

    [StringLength(50)]
    public string? Model { get; set; }

    [Required]
    public int SeatCapacity { get; set; }

    [StringLength(20)]
    public string? Transmission { get; set; }

    [StringLength(20)]
    public string? FuelType { get; set; }

    [Required]
    [StringLength(20)]
    public string StatusName { get; set; } = "Active";

    [StringLength(300)]
    public string? ImageUrl { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    // Navigation properties
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
    public ICollection<VehicleHistory> VehicleHistories { get; set; } = new List<VehicleHistory>();
}