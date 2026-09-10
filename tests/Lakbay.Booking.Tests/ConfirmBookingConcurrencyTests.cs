using Lakbay.Booking.Api.Application.Commands;
using Lakbay.Booking.Api.Domain;
using Lakbay.Booking.Api.Payments;
using Microsoft.EntityFrameworkCore;

namespace Lakbay.Booking.Tests;

/// <summary>
/// ADR-0011, required not optional: "two simulated simultaneous
/// confirm-booking calls against a slot with AvailableCount = 1,
/// asserting exactly one succeeds." Runs against a real, disposable
/// LocalDB database (not InMemory/SQLite) because the guarantee being
/// tested is SQL Server's own row-level locking on the conditional
/// UPDATE — a fake provider can't reproduce that.
///
/// ADR-0026 consequence: this must not be exempted for the Agent channel
/// — it exercises the Agent path specifically, since that's the one
/// channel that can actually complete end-to-end today.
/// </summary>
public sealed class ConfirmBookingConcurrencyTests : IAsyncLifetime
{
    private readonly LocalDbFixture _fixture = new();

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task Exactly_one_of_two_simultaneous_confirms_succeeds_when_AvailableCount_is_one()
    {
        const string productId = "coron-island-hopping-concurrency";
        var dateSlot = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1));

        await using (var seedDb = _fixture.CreateContext())
        {
            seedDb.AvailabilitySlots.Add(AvailabilitySlot.Seed(productId, dateSlot, availableCount: 1));
            await seedDb.SaveChangesAsync();
        }

        // Each concurrent "request" gets its own DbContext/connection —
        // simulating two separate simultaneous HTTP requests, not two
        // calls sharing one connection (which wouldn't prove anything
        // about database-level locking).
        Task<ConfirmBookingResult> ConfirmAsync(string customerId)
        {
            var db = _fixture.CreateContext();
            var handler = new ConfirmBookingCommandHandler(db, new NeverCalledPaymentGateway());
            return handler.Handle(
                new ConfirmBookingCommand(productId, dateSlot, customerId, Channel.Agent),
                CancellationToken.None);
        }

        var firstRequest = ConfirmAsync("customer-a");
        var secondRequest = ConfirmAsync("customer-b");

        var results = await Task.WhenAll(firstRequest, secondRequest);

        var confirmedCount = results.Count(r => r.Status == ConfirmBookingStatus.Confirmed);
        var notAvailableCount = results.Count(r => r.Status == ConfirmBookingStatus.NotAvailable);

        Assert.Equal(1, confirmedCount);
        Assert.Equal(1, notAvailableCount);

        // The database itself must agree: exactly one booking row, and
        // availability landed at 0, never negative (which a broken
        // read-then-write race could produce) and never still 1 (which
        // would mean both requests were wrongly rejected or both wrongly
        // silently no-opped).
        await using var verifyDb = _fixture.CreateContext();
        var finalSlot = await verifyDb.AvailabilitySlots.SingleAsync(s => s.ProductId == productId);
        Assert.Equal(0, finalSlot.AvailableCount);

        var bookingCount = await verifyDb.Bookings.CountAsync(b => b.ProductId == productId);
        Assert.Equal(1, bookingCount);
    }

    [Fact]
    public async Task Ten_simultaneous_confirms_against_AvailableCount_three_yield_exactly_three_successes()
    {
        // A stronger version of the ADR-0011 guarantee at higher
        // concurrency, to rule out "got lucky with just two requests."
        const string productId = "boracay-getaway-concurrency";
        var dateSlot = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(2));

        await using (var seedDb = _fixture.CreateContext())
        {
            seedDb.AvailabilitySlots.Add(AvailabilitySlot.Seed(productId, dateSlot, availableCount: 3));
            await seedDb.SaveChangesAsync();
        }

        Task<ConfirmBookingResult> ConfirmAsync(int i)
        {
            var db = _fixture.CreateContext();
            var handler = new ConfirmBookingCommandHandler(db, new NeverCalledPaymentGateway());
            return handler.Handle(
                new ConfirmBookingCommand(productId, dateSlot, $"customer-{i}", Channel.Agent),
                CancellationToken.None);
        }

        var tasks = Enumerable.Range(0, 10).Select(ConfirmAsync).ToArray();
        var results = await Task.WhenAll(tasks);

        Assert.Equal(3, results.Count(r => r.Status == ConfirmBookingStatus.Confirmed));
        Assert.Equal(7, results.Count(r => r.Status == ConfirmBookingStatus.NotAvailable));

        await using var verifyDb = _fixture.CreateContext();
        var finalSlot = await verifyDb.AvailabilitySlots.SingleAsync(s => s.ProductId == productId);
        Assert.Equal(0, finalSlot.AvailableCount);
        Assert.Equal(3, await verifyDb.Bookings.CountAsync(b => b.ProductId == productId));
    }

    private sealed class NeverCalledPaymentGateway : IPaymentGateway
    {
        public Task<PaymentChargeResult> ChargeAsync(PaymentChargeRequest request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Agent channel must never call the payment gateway (ADR-0026).");
    }
}
