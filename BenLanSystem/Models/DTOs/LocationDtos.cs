using System.ComponentModel.DataAnnotations;

namespace BenLanSystem.Models.DTOs;

public class LocationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Province { get; set; }
    public string? AddressLine { get; set; }
    public bool IsActive { get; set; }
}

public class LocationCreateDto
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Province { get; set; }

    [StringLength(200)]
    public string? AddressLine { get; set; }

    public bool IsActive { get; set; } = true;
}

public class LocationUpdateDto
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Province { get; set; }

    [StringLength(200)]
    public string? AddressLine { get; set; }

    public bool IsActive { get; set; } = true;
}