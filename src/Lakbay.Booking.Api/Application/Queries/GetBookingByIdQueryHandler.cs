using Lakbay.Booking.Api.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lakbay.Booking.Api.Application.Queries;

public sealed class GetBookingByIdQueryHandler(BookingDbContext db) : IRequestHandler<GetBookingByIdQuery, BookingDto?>
{
    public async Task<BookingDto?> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);

        return booking is null
            ? null
            : new BookingDto(
                booking.Id,
                booking.ProductId,
                booking.DateSlot,
                booking.CustomerId,
                booking.Channel.ToString(),
                booking.PaymentStatus.ToString(),
                booking.CreatedAtUtc);
    }
}
