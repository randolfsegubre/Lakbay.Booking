using Lakbay.Booking.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Lakbay.Booking.Api.Infrastructure;

/// <summary>
/// Seeds a couple of ProductId/DateSlot rows on startup so the confirm
/// flow can be exercised end-to-end without Lakbay.Cms/Lakbay.AvailabilityApi
/// running. Idempotent — only inserts rows that don't already exist by
/// ProductId+DateSlot, so restarting the app doesn't reset counts an
/// earlier `dotnet run` already decremented.
/// </summary>
public static class AvailabilitySeeder
{
    public static async Task SeedAsync(BookingDbContext db, CancellationToken cancellationToken = default)
    {
        var seedRows = new[]
        {
            (ProductId: "coron-island-hopping", DateSlot: DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(7)), AvailableCount: 5),
            (ProductId: "boracay-getaway", DateSlot: DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(14)), AvailableCount: 1)
        };

        foreach (var row in seedRows)
        {
            var exists = await db.AvailabilitySlots.AnyAsync(
                s => s.ProductId == row.ProductId && s.DateSlot == row.DateSlot,
                cancellationToken);

            if (!exists)
            {
                db.AvailabilitySlots.Add(AvailabilitySlot.Seed(row.ProductId, row.DateSlot, row.AvailableCount));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
