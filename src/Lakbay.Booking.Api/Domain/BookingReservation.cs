namespace Lakbay.Booking.Api.Domain;

/// <summary>
/// A confirmed booking. Per the Architecture &amp; Patterns Guide's
/// encapsulation row, this type owns its own invariants — it is never
/// constructed with a public setter dance; <see cref="Confirm"/> is the
/// only way to create one, and it always represents a booking that has
/// already passed the atomic availability decrement (ADR-0011).
/// </summary>
/// <remarks>
/// Named <c>BookingReservation</c>, not <c>Booking</c> — this project's
/// own root namespace is <c>Lakbay.Booking.Api</c>, which makes
/// <c>Lakbay.Booking</c> an ancestor namespace of every type here. A type
/// literally named <c>Booking</c> would collide with that namespace
/// segment in C#'s name-lookup order (namespace members are found while
/// walking outward through enclosing namespaces, before usings are ever
/// consulted) and fail to compile as "a namespace but is used like a
/// type." This is a project-structural fact, not a style choice.
/// </remarks>
public sealed class BookingReservation
{
    public Guid Id { get; private set; }
    public string ProductId { get; private set; } = default!;
    public DateOnly DateSlot { get; private set; }
    public string CustomerId { get; private set; } = default!;
    public Channel Channel { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    // EF Core materialization only.
    private BookingReservation()
    {
    }

    /// <summary>
    /// Creates a booking record. Callers (the command handler) must only
    /// call this AFTER the atomic availability decrement has already
    /// succeeded — this type has no way to check availability itself,
    /// by design, since that check must be a single database statement,
    /// not something re-derived here.
    /// </summary>
    public static BookingReservation Confirm(
        string productId,
        DateOnly dateSlot,
        string customerId,
        Channel channel,
        PaymentStatus paymentStatus)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException("Product id is required.", nameof(productId));
        }

        if (string.IsNullOrWhiteSpace(customerId))
        {
            throw new ArgumentException("Customer id is required.", nameof(customerId));
        }

        return new BookingReservation
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            DateSlot = dateSlot,
            CustomerId = customerId,
            Channel = channel,
            PaymentStatus = paymentStatus,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
