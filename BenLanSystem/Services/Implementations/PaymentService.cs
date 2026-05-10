using BenLanSystem.Data;
using BenLanSystem.Models.DTOs;
using BenLanSystem.Models.Entities;
using BenLanSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BenLanSystem.Services.Implementations;

public class PaymentService(ApplicationDbContext db) : IPaymentService
{
    public async Task<PaymentDto> CreateAsync(PaymentCreateDto dto)
    {
        var existing = await db.Payments.FirstOrDefaultAsync(p => p.BookingId == dto.BookingId);
        if (existing is not null)
        {
            if (existing.PaymentStatus is "Paid" or "Refunded")
                throw new InvalidOperationException($"Booking {dto.BookingId} already has a {existing.PaymentStatus.ToLowerInvariant()} payment.");

            existing.Amount = dto.Amount;
            existing.PaymentMethod = dto.PaymentMethod;
            existing.TransactionRef = dto.TransactionRef;
            await db.SaveChangesAsync();
            return (await GetByIdAsync(existing.Id))!;
        }

        var payment = new Payment
        {
            BookingId = dto.BookingId, Amount = dto.Amount,
            PaymentMethod = dto.PaymentMethod, TransactionRef = dto.TransactionRef,
            PaymentStatus = "Pending"
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        return (await GetByIdAsync(payment.Id))!;
    }

    public async Task<PaymentDto?> GetByIdAsync(long id)
    {
        var p = await db.Payments.FindAsync(id);
        if (p is null) return null;
        return new PaymentDto
        {
            Id = p.Id, BookingId = p.BookingId, Amount = p.Amount,
            PaymentMethod = p.PaymentMethod, PaymentStatus = p.PaymentStatus,
            TransactionRef = p.TransactionRef, PaidAtUtc = p.PaidAtUtc, CreatedAtUtc = p.CreatedAtUtc
        };
    }

    public async Task<IEnumerable<PaymentDto>> GetByBookingAsync(long bookingId)
        => await db.Payments.Where(p => p.BookingId == bookingId)
            .Select(p => new PaymentDto
            {
                Id = p.Id, BookingId = p.BookingId, Amount = p.Amount,
                PaymentMethod = p.PaymentMethod, PaymentStatus = p.PaymentStatus,
                TransactionRef = p.TransactionRef, PaidAtUtc = p.PaidAtUtc, CreatedAtUtc = p.CreatedAtUtc
            }).ToListAsync();

    public async Task<IEnumerable<PaymentDto>> GetAllAsync(int page = 1, int pageSize = 50)
        => await db.Payments.OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new PaymentDto
            {
                Id = p.Id, BookingId = p.BookingId, Amount = p.Amount,
                PaymentMethod = p.PaymentMethod, PaymentStatus = p.PaymentStatus,
                TransactionRef = p.TransactionRef, PaidAtUtc = p.PaidAtUtc, CreatedAtUtc = p.CreatedAtUtc
            }).ToListAsync();

    public async Task<PaymentDto> MarkAsPaidAsync(long id, PaymentMarkPaidDto dto)
    {
        var payment = await db.Payments.FindAsync(id) ?? throw new KeyNotFoundException($"Payment {id} not found");
        if (payment.PaymentStatus != "Pending")
            throw new InvalidOperationException($"Cannot mark as paid; current status is '{payment.PaymentStatus}'");

        var oldStatus = payment.PaymentStatus;
        payment.PaymentStatus = "Paid";
        payment.PaidAtUtc = DateTime.UtcNow;
        if (dto.TransactionRef is not null) payment.TransactionRef = dto.TransactionRef;

        db.PaymentHistories.Add(new PaymentHistory
        {
            PaymentId = payment.Id, ChangeDate = DateTime.UtcNow, ChangedBy = "System",
            OldStatus = oldStatus, NewStatus = "Paid"
        });

        await db.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<PaymentDto> RefundAsync(long id, PaymentRefundDto dto)
    {
        var payment = await db.Payments.FindAsync(id) ?? throw new KeyNotFoundException($"Payment {id} not found");
        if (payment.PaymentStatus != "Paid")
            throw new InvalidOperationException($"Cannot refund; current status is '{payment.PaymentStatus}'");

        var oldStatus = payment.PaymentStatus;
        payment.PaymentStatus = "Refunded";

        db.PaymentHistories.Add(new PaymentHistory
        {
            PaymentId = payment.Id, ChangeDate = DateTime.UtcNow, ChangedBy = "System",
            OldStatus = oldStatus, NewStatus = "Refunded", Remarks = dto.Reason
        });

        await db.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }
}
