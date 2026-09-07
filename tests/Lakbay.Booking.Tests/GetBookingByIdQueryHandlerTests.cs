using Lakbay.Booking.Api.Application.Queries;
using Lakbay.Booking.Api.Domain;

namespace Lakbay.Booking.Tests;

public sealed class GetBookingByIdQueryHandlerTests : IAsyncLifetime
{
    private readonly LocalDbFixture _fixture = new();

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task Returns_booking_dto_when_it_exists()
    {
        var booking = BookingReservation.Confirm(
            "prod-x", DateOnly.FromDateTime(DateTime.UtcNow.Date), "cust-x", Channel.Agent, PaymentStatus.PendingInvoice);

        await using (var seedDb = _fixture.CreateContext())
        {
            seedDb.Bookings.Add(booking);
            await seedDb.SaveChangesAsync();
        }

        await using var db = _fixture.CreateContext();
        var handler = new GetBookingByIdQueryHandler(db);

        var dto = await handler.Handle(new GetBookingByIdQuery(booking.Id), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(booking.Id, dto!.Id);
        Assert.Equal("prod-x", dto.ProductId);
        Assert.Equal("Agent", dto.Channel);
        Assert.Equal("PendingInvoice", dto.PaymentStatus);
    }

    [Fact]
    public async Task Returns_null_when_booking_does_not_exist()
    {
        await using var db = _fixture.CreateContext();
        var handler = new GetBookingByIdQueryHandler(db);

        var dto = await handler.Handle(new GetBookingByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(dto);
    }
}
