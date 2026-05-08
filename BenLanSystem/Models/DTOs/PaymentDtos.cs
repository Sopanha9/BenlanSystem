using System.ComponentModel.DataAnnotations;

namespace BenLanSystem.Models.DTOs;

public class PaymentDto
{
    public long Id { get; set; }
    public long BookingId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = "Pending";
    public string? TransactionRef { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class PaymentCreateDto
{
    [Required]
    public long BookingId { get; set; }

    [Required, Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required, StringLength(30)]
    public string PaymentMethod { get; set; } = string.Empty;

    [StringLength(100)]
    public string? TransactionRef { get; set; }
}

public class PaymentMarkPaidDto
{
    [StringLength(100)]
    public string? TransactionRef { get; set; }
}

public class PaymentRefundDto
{
    [StringLength(500)]
    public string? Reason { get; set; }
}