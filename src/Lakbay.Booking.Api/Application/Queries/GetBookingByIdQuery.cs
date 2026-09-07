using MediatR;

namespace Lakbay.Booking.Api.Application.Queries;

/// <summary>
/// ADR-0002: reads are a separate MediatR request from writes — this
/// query has no side effects and no relation to
/// <c>ConfirmBookingCommandHandler</c> beyond reading what it wrote.
/// </summary>
public sealed record GetBookingByIdQuery(Guid Id) : IRequest<BookingDto?>;

public sealed record BookingDto(
    Guid Id,
    string ProductId,
    DateOnly DateSlot,
    string CustomerId,
    string Channel,
    string PaymentStatus,
    DateTime CreatedAtUtc);
