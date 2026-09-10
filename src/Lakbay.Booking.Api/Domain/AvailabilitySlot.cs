namespace Lakbay.Booking.Api.Domain;

/// <summary>
/// A bookable ProductId/DateSlot pair with a remaining count. This is the
/// table ADR-0011's atomic UPDATE targets. Deliberately dumb — it exposes
/// no in-process "decrement" method, because the decrement is required to
/// be a single database statement (see
/// <c>ConfirmBookingCommandHandler</c>), not something computed here and
/// then written back with a separate call.
/// </summary>
public sealed class AvailabilitySlot
{
    public int Id { get; private set; }
    public string ProductId { get; private set; } = default!;
    public DateOnly DateSlot { get; private set; }
    public int AvailableCount { get; private set; }

    // EF Core materialization only.
    private AvailabilitySlot()
    {
    }

    /// <summary>Used only by seed/test data — never by the confirm path.</summary>
    public static AvailabilitySlot Seed(string productId, DateOnly dateSlot, int availableCount)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException("Product id is required.", nameof(productId));
        }

        if (availableCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(availableCount), "Available count cannot be negative.");
        }

        return new AvailabilitySlot
        {
            ProductId = productId,
            DateSlot = dateSlot,
            AvailableCount = availableCount
        };
    }
}
