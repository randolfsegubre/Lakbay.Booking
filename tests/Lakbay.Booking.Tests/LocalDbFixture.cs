using Lakbay.Booking.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Lakbay.Booking.Tests;

/// <summary>
/// ADR-0011's guarantee is about real database engine row-locking
/// behavior — the EF Core InMemory provider doesn't enforce transactions
/// or locking the same way, and doesn't support <c>ExecuteSqlInterpolated</c>
/// at all, so it cannot prove anything about this ADR. Tests that exercise
/// <c>ConfirmBookingCommandHandler</c> run against a real, disposable
/// LocalDB database instead — one per test class, dropped afterward.
/// </summary>
public sealed class LocalDbFixture : IAsyncLifetime
{
    public string ConnectionString { get; } =
        $"Server=(localdb)\\mssqllocaldb;Database=LakbayBookingTests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True";

    public DbContextOptions<BookingDbContext> Options { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        await using var db = new BookingDbContext(Options);
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var db = new BookingDbContext(Options);
        await db.Database.EnsureDeletedAsync();
    }

    public BookingDbContext CreateContext() => new(Options);
}
