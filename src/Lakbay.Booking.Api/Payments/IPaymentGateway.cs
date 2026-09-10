namespace Lakbay.Booking.Api.Payments;

/// <summary>
/// Strategy interface for charging a customer at confirm time — used only
/// by <see cref="Domain.Channel.Online"/> bookings. Agent-channel bookings
/// (ADR-0026) never call this at all; they go straight to
/// <see cref="Domain.PaymentStatus.PendingInvoice"/>.
/// </summary>
public interface IPaymentGateway
{
    Task<PaymentChargeResult> ChargeAsync(PaymentChargeRequest request, CancellationToken cancellationToken);
}

public sealed record PaymentChargeRequest(string ProductId, string CustomerId, decimal AmountPhp);

public sealed record PaymentChargeResult(bool Succeeded, string? ReferenceId);
