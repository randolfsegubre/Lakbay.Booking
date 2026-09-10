namespace Lakbay.Booking.Api.Payments;

/// <summary>
/// The real PayMongo implementation is blocked on a sandbox account that
/// doesn't exist yet (ADR-0026, Lakbay.Docs/docs/04_TASKS.md). This stub
/// exists so the CQRS/DI shape for the Online channel is real and
/// correct — registered in the composition root, actually reachable from
/// the command handler — without pretending a charge happened. ADR-0026
/// explicitly rejected a fake "successful payment" implementation here:
/// that would misrepresent the Online channel as done when it isn't.
/// </summary>
public sealed class PayMongoPaymentGateway : IPaymentGateway
{
    public Task<PaymentChargeResult> ChargeAsync(PaymentChargeRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException(
            "PayMongo integration is not implemented yet — blocked on a sandbox account " +
            "that doesn't exist (ADR-0026). Channel=Online bookings cannot be confirmed " +
            "until this is built; use Channel=Agent for a real end-to-end confirm flow.");
    }
}
