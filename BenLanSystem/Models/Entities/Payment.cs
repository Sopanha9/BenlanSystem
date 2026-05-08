using System.ComponentModel.DataAnnotations;

namespace BenLanSystem.Models.Entities;

public class Payment
{
    public long Id { get; set; }

    [Required]
    public long BookingId { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(30)]
    public string PaymentMethod { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string PaymentStatus { get; set; } = "Pending";

    [StringLength(100)]
    public string? TransactionRef { get; set; }

    public DateTime? PaidAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Booking Booking { get; set; } = null!;
    public ICollection<PaymentHistory> PaymentHistories { get; set; } = new List<PaymentHistory>();
}