using Lakbay.Booking.Api.Payments;

namespace Lakbay.Booking.Tests;

/// <summary>
/// Test-only stand-in for the Online channel's gateway so handler tests
/// can exercise the "Online succeeds" path without touching PayMongo
/// (which, per ADR-0026, isn't implemented at all yet in the real code —
/// <see cref="Api.Payments.PayMongoPaymentGateway"/> always throws). Never
/// used by application code, only by tests.
/// </summary>
public sealed class FakePaymentGateway(bool succeeds = true) : IPaymentGateway
{
    public Task<PaymentChargeResult> ChargeAsync(PaymentChargeRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new PaymentChargeResult(succeeds, succeeds ? "fake-ref-123" : null));
}
