using System.ComponentModel.DataAnnotations;

namespace BenLanSystem.Models.Entities;

public class VehicleHistory
{
    public long Id { get; set; }

    [Required]
    public int VehicleId { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    [StringLength(100)]
    public string ActionType { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(100)]
    public string? PerformedBy { get; set; }

    // Navigation properties
    public Vehicle Vehicle { get; set; } = null!;
}