using Lakbay.Booking.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Lakbay.Booking.Api.Infrastructure;

/// <summary>
/// EF Core context for Lakbay.Booking's own database (ADR-0003 — kept
/// entirely separate from Umbraco's). SQL Server locally (LocalDB by
/// default — see Program.cs/appsettings for the connection string), SQL
/// Server/Azure SQL in a real environment (ADR-0005).
/// </summary>
public sealed class BookingDbContext(DbContextOptions<BookingDbContext> options) : DbContext(options)
{
    public DbSet<BookingReservation> Bookings => Set<BookingReservation>();

    public DbSet<AvailabilitySlot> AvailabilitySlots => Set<AvailabilitySlot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BookingReservation>(entity =>
        {
            entity.ToTable("Bookings");
            entity.HasKey(b => b.Id);
            entity.Property(b => b.ProductId).IsRequired().HasMaxLength(100);
            entity.Property(b => b.CustomerId).IsRequired().HasMaxLength(100);
            entity.Property(b => b.Channel)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(b => b.PaymentStatus)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            entity.HasIndex(b => new { b.ProductId, b.DateSlot });
        });

        modelBuilder.Entity<AvailabilitySlot>(entity =>
        {
            entity.ToTable("AvailabilitySlots");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.ProductId).IsRequired().HasMaxLength(100);
            // What ADR-0011's atomic UPDATE targets by ProductId+DateSlot —
            // must stay unique or the conditional UPDATE could touch more
            // than one row.
            entity.HasIndex(s => new { s.ProductId, s.DateSlot }).IsUnique();
        });
    }
}
