using System.ComponentModel.DataAnnotations;

namespace BenLanSystem.Models.DTOs;

public class VehicleDto
{
    public int Id { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int SeatCapacity { get; set; }
    public string? Transmission { get; set; }
    public string? FuelType { get; set; }
    public string StatusName { get; set; } = "Active";
    public string? ImageUrl { get; set; }
}

public class VehicleCreateDto
{
    [Required, StringLength(20)]
    public string PlateNumber { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Brand { get; set; }

    [StringLength(50)]
    public string? Model { get; set; }

    [Required, Range(1, 200)]
    public int SeatCapacity { get; set; }

    [StringLength(20)]
    public string? Transmission { get; set; }

    [StringLength(20)]
    public string? FuelType { get; set; }

    [StringLength(20)]
    public string StatusName { get; set; } = "Active";

    [StringLength(300)]
    public string? ImageUrl { get; set; }
}

public class VehicleUpdateDto
{
    [StringLength(20)]
    public string? PlateNumber { get; set; }

    [StringLength(50)]
    public string? Brand { get; set; }

    [StringLength(50)]
    public string? Model { get; set; }

    [Range(1, 200)]
    public int? SeatCapacity { get; set; }

    [StringLength(20)]
    public string? Transmission { get; set; }

    [StringLength(20)]
    public string? FuelType { get; set; }

    [StringLength(20)]
    public string? StatusName { get; set; }

    [StringLength(300)]
    public string? ImageUrl { get; set; }
}