using Lakbay.Booking.Api.Domain;

namespace Lakbay.Booking.Api.Api;

/// <summary>Request body for POST /api/bookings/confirm.</summary>
public sealed record ConfirmBookingRequest(
    string ProductId,
    DateOnly DateSlot,
    string CustomerId,
    Channel Channel);
