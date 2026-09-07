using Lakbay.Booking.Api.Domain;
using MediatR;

namespace Lakbay.Booking.Api.Application.Commands;

/// <summary>
/// ADR-0002: a command, handled by its own handler — never a method on a
/// shared "BookingService". Carries <see cref="Channel"/> per ADR-0026 so
/// the handler can branch on payment handling (Agent vs. Online) while
/// running the exact same atomic availability decrement for both.
/// </summary>
public sealed record ConfirmBookingCommand(
    string ProductId,
    DateOnly DateSlot,
    string CustomerId,
    Channel Channel) : IRequest<ConfirmBookingResult>;
