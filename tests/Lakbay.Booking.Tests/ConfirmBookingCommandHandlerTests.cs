using Lakbay.Booking.Api.Application.Commands;
using Lakbay.Booking.Api.Domain;
using Lakbay.Booking.Api.Payments;
using Microsoft.EntityFrameworkCore;

namespace Lakbay.Booking.Tests;

/// <summary>
/// Sequential-call coverage for ADR-0002 (real CQRS handler, not a fat
/// service) and ADR-0026 (Channel decides payment handling only). The
/// concurrency-specific guarantee from ADR-0011 has its own dedicated
/// test class — <see cref="ConfirmBookingConcurrencyTests"/> — since a
/// sequential test cannot catch a race condition.
/// </summary>
public sealed class ConfirmBookingCommandHandlerTests : IAsyncLifetime
{
    private readonly LocalDbFixture _fixture = new();

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task Agent_channel_sets_PendingInvoice_and_decrements_availability_without_calling_gateway()
    {
        var dateSlot = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        await using (var seedDb = _fixture.CreateContext())
        {
            seedDb.AvailabilitySlots.Add(AvailabilitySlot.Seed("prod-agent", dateSlot, 3));
            await seedDb.SaveChangesAsync();
        }

        await using var db = _fixture.CreateContext();
        var handler = new ConfirmBookingCommandHandler(db, new ThrowingPaymentGateway());

        var result = await handler.Handle(
            new ConfirmBookingCommand("prod-agent", dateSlot, "cust-1", Channel.Agent),
            CancellationToken.None);

        Assert.Equal(ConfirmBookingStatus.Confirmed, result.Status);
        Assert.Equal(PaymentStatus.PendingInvoice, result.PaymentStatus);
        Assert.NotNull(result.BookingId);

        await using var verifyDb = _fixture.CreateContext();
        var slot = await verifyDb.AvailabilitySlots.SingleAsync(s => s.ProductId == "prod-agent");
        Assert.Equal(2, slot.AvailableCount);

        var booking = await verifyDb.Bookings.SingleAsync(b => b.Id == result.BookingId);
        Assert.Equal(Channel.Agent, booking.Channel);
        Assert.Equal(PaymentStatus.PendingInvoice, booking.PaymentStatus);
    }

    [Fact]
    public async Task When_AvailableCount_is_zero_returns_NotAvailable_and_creates_no_booking()
    {
        var dateSlot = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        await using (var seedDb = _fixture.CreateContext())
        {
            seedDb.AvailabilitySlots.Add(AvailabilitySlot.Seed("prod-soldout", dateSlot, 0));
            await seedDb.SaveChangesAsync();
        }

        await using var db = _fixture.CreateContext();
        var handler = new ConfirmBookingCommandHandler(db, new ThrowingPaymentGateway());

        var result = await handler.Handle(
            new ConfirmBookingCommand("prod-soldout", dateSlot, "cust-1", Channel.Agent),
            CancellationToken.None);

        Assert.Equal(ConfirmBookingStatus.NotAvailable, result.Status);
        Assert.Null(result.BookingId);

        await using var verifyDb = _fixture.CreateContext();
        Assert.Equal(0, await verifyDb.Bookings.CountAsync());
    }

    [Fact]
    public async Task Online_channel_calls_gateway_and_records_Paid_on_success()
    {
        var dateSlot = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        await using (var seedDb = _fixture.CreateContext())
        {
            seedDb.AvailabilitySlots.Add(AvailabilitySlot.Seed("prod-online", dateSlot, 1));
            await seedDb.SaveChangesAsync();
        }

        await using var db = _fixture.CreateContext();
        var handler = new ConfirmBookingCommandHandler(db, new FakePaymentGateway(succeeds: true));

        var result = await handler.Handle(
            new ConfirmBookingCommand("prod-online", dateSlot, "cust-1", Channel.Online),
            CancellationToken.None);

        Assert.Equal(ConfirmBookingStatus.Confirmed, result.Status);
        Assert.Equal(PaymentStatus.Paid, result.PaymentStatus);
    }

    [Fact]
    public async Task Online_channel_with_real_PayMongo_stub_rolls_back_decrement_and_returns_NotImplemented()
    {
        // Proves ADR-0026's "leave it stubbed, don't fake it" requirement:
        // the real IPaymentGateway implementation (PayMongoPaymentGateway)
        // throws NotImplementedException, and the handler must not leave
        // availability decremented or a booking half-created behind it.
        var dateSlot = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        await using (var seedDb = _fixture.CreateContext())
        {
            seedDb.AvailabilitySlots.Add(AvailabilitySlot.Seed("prod-online-real", dateSlot, 1));
            await seedDb.SaveChangesAsync();
        }

        await using var db = _fixture.CreateContext();
        var handler = new ConfirmBookingCommandHandler(db, new PayMongoPaymentGateway());

        var result = await handler.Handle(
            new ConfirmBookingCommand("prod-online-real", dateSlot, "cust-1", Channel.Online),
            CancellationToken.None);

        Assert.Equal(ConfirmBookingStatus.PaymentGatewayNotImplemented, result.Status);

        await using var verifyDb = _fixture.CreateContext();
        var slot = await verifyDb.AvailabilitySlots.SingleAsync(s => s.ProductId == "prod-online-real");
        Assert.Equal(1, slot.AvailableCount); // unchanged — rolled back
        Assert.Equal(0, await verifyDb.Bookings.CountAsync());
    }

    /// <summary>Fails the test loudly if the Agent path ever calls the gateway.</summary>
    private sealed class ThrowingPaymentGateway : IPaymentGateway
    {
        public Task<PaymentChargeResult> ChargeAsync(PaymentChargeRequest request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Agent channel must never call the payment gateway (ADR-0026).");
    }
}
