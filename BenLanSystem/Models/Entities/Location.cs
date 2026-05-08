using System.ComponentModel.DataAnnotations;

namespace BenLanSystem.Models.Entities;

public class Location
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Province { get; set; }

    [StringLength(200)]
    public string? AddressLine { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Route> OriginRoutes { get; set; } = new List<Route>();
    public ICollection<Route> DestinationRoutes { get; set; } = new List<Route>();
}