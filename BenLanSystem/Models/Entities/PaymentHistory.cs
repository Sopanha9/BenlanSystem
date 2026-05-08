using System.ComponentModel.DataAnnotations;

namespace BenLanSystem.Models.Entities;

public class PaymentHistory
{
    public long Id { get; set; }

    [Required]
    public long PaymentId { get; set; }

    [Required]
    public DateTime ChangeDate { get; set; }

    [Required]
    [StringLength(100)]
    public string ChangedBy { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string OldStatus { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string NewStatus { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Remarks { get; set; }

    // Navigation properties
    public Payment Payment { get; set; } = null!;
}