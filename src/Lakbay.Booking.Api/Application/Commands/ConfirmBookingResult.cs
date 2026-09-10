using Lakbay.Booking.Api.Domain;

namespace Lakbay.Booking.Api.Application.Commands;

public enum ConfirmBookingStatus
{
    Confirmed,

    /// <summary>
    /// The atomic decrement (ADR-0011) affected 0 rows — nothing was
    /// available. Not an error: a legitimate, expected outcome of a lost
    /// race for the last slot.
    /// </summary>
    NotAvailable,

    /// <summary>
    /// Channel=Online was requested but the payment gateway isn't
    /// implemented yet (ADR-0026 — blocked on a PayMongo sandbox
    /// account). No availability was consumed — the decrement, if it had
    /// already happened, is rolled back before this is returned.
    /// </summary>
    PaymentGatewayNotImplemented
}

public sealed record ConfirmBookingResult(
    ConfirmBookingStatus Status,
    Guid? BookingId,
    PaymentStatus? PaymentStatus,
    string Message)
{
    public static ConfirmBookingResult Confirmed(Guid bookingId, PaymentStatus paymentStatus) =>
        new(ConfirmBookingStatus.Confirmed, bookingId, paymentStatus, "Booking confirmed.");

    public static ConfirmBookingResult NotAvailable() =>
        new(ConfirmBookingStatus.NotAvailable, null, null, "This slot is no longer available.");

    public static ConfirmBookingResult PaymentGatewayNotImplemented(string reason) =>
        new(ConfirmBookingStatus.PaymentGatewayNotImplemented, null, null, reason);
}
