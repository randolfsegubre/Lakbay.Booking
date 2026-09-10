namespace Lakbay.Booking.Api.Domain;

/// <summary>
/// Payment state of a confirmed booking. <see cref="PendingInvoice"/> is
/// the ADR-0026 state: set directly for <see cref="Channel.Agent"/>
/// bookings, with no gateway call at all — a follow-up invoice/payment
/// link is sent asynchronously by Lakbay.AgentOps (ADR-0025).
/// </summary>
public enum PaymentStatus
{
    /// <summary>Charge attempted via <see cref="Payments.IPaymentGateway"/> but not yet resolved.</summary>
    Pending,

    /// <summary>
    /// Agent-channel deferred payment: no gateway call happened, the
    /// customer will be invoiced/sent a payment link afterward.
    /// </summary>
    PendingInvoice,

    /// <summary>Charged successfully via the payment gateway (Online channel).</summary>
    Paid,

    /// <summary>Gateway charge attempted and declined/failed (Online channel).</summary>
    Failed
}
