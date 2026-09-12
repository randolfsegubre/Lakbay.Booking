using System.Net;
using System.Net.Http;
using Grpc.Net.Client;
using Lakbay.Booking.Api.Domain;
using Lakbay.Booking.Api.Grpc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Lakbay.Booking.Tests;

/// <summary>
/// ADR-0027, proven end-to-end rather than only compiling: a real gRPC
/// client (Grpc.Net.Client — the same package Lakbay.AgentOps uses)
/// calling the actual <see cref="BookingConfirmGrpcService"/> hosted
/// in-process by <see cref="WebApplicationFactory{Program}"/>, backed by a
/// real LocalDB database (never InMemory/SQLite — see
/// <see cref="LocalDbFixture"/>), so the ADR-0011 atomic decrement really
/// runs. This is what "real and running" means per the standing note in
/// memory's job_search_gkp_2026.md — no gRPC claim goes on the resume
/// until this test exists and passes.
/// </summary>
public sealed class BookingConfirmGrpcServiceTests : IAsyncLifetime
{
    private readonly LocalDbFixture _dbFixture = new();
    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await _dbFixture.InitializeAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    // Point the app under test at this test class's own
                    // disposable LocalDB database instead of appsettings'
                    // default, same as LocalDbFixture's own reasoning.
                    ["ConnectionStrings:BookingDb"] = _dbFixture.ConnectionString,
                })));
    }

    public async Task DisposeAsync()
    {
        _factory.Dispose();
        await _dbFixture.DisposeAsync();
    }

    private BookingConfirmService.BookingConfirmServiceClient CreateGrpcClient()
    {
        var httpClient = _factory.CreateClient();

        // gRPC is HTTP/2-only; TestServer's in-memory transport needs the
        // client to explicitly demand it rather than negotiating.
        httpClient.DefaultRequestVersion = HttpVersion.Version20;
        httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;

        var channel = GrpcChannel.ForAddress(httpClient.BaseAddress!, new GrpcChannelOptions
        {
            HttpClient = httpClient,
        });

        return new BookingConfirmService.BookingConfirmServiceClient(channel);
    }

    [Fact]
    public async Task ConfirmAgentBooking_over_a_real_grpc_call_confirms_and_decrements_availability()
    {
        const string productId = "el-nido-island-hopping-grpc";
        var dateSlot = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(3));

        await using (var seedDb = _dbFixture.CreateContext())
        {
            seedDb.AvailabilitySlots.Add(AvailabilitySlot.Seed(productId, dateSlot, availableCount: 1));
            await seedDb.SaveChangesAsync();
        }

        var grpcClient = CreateGrpcClient();

        var reply = await grpcClient.ConfirmAgentBookingAsync(new ConfirmAgentBookingRequest
        {
            ProductId = productId,
            DateSlot = dateSlot.ToString("yyyy-MM-dd"),
            CustomerId = "customer-grpc-test",
        });

        Assert.Equal(ConfirmAgentBookingStatus.Confirmed, reply.Status);
        Assert.True(Guid.TryParse(reply.BookingId, out _));
        // ADR-0026: Agent channel is deferred-payment, never a gateway charge.
        Assert.Equal(nameof(PaymentStatus.PendingInvoice), reply.PaymentStatus);

        await using var verifyDb = _dbFixture.CreateContext();
        var finalSlot = await verifyDb.AvailabilitySlots.SingleAsync(s => s.ProductId == productId);
        Assert.Equal(0, finalSlot.AvailableCount);

        var booking = await verifyDb.Bookings.SingleAsync(b => b.ProductId == productId);
        Assert.Equal(Channel.Agent, booking.Channel);
        Assert.Equal(PaymentStatus.PendingInvoice, booking.PaymentStatus);
    }

    [Fact]
    public async Task ConfirmAgentBooking_returns_NotAvailable_status_over_grpc_when_the_slot_is_sold_out()
    {
        const string productId = "el-nido-island-hopping-grpc-soldout";
        var dateSlot = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(4));

        await using (var seedDb = _dbFixture.CreateContext())
        {
            seedDb.AvailabilitySlots.Add(AvailabilitySlot.Seed(productId, dateSlot, availableCount: 0));
            await seedDb.SaveChangesAsync();
        }

        var grpcClient = CreateGrpcClient();

        var reply = await grpcClient.ConfirmAgentBookingAsync(new ConfirmAgentBookingRequest
        {
            ProductId = productId,
            DateSlot = dateSlot.ToString("yyyy-MM-dd"),
            CustomerId = "customer-grpc-test-2",
        });

        Assert.Equal(ConfirmAgentBookingStatus.NotAvailable, reply.Status);

        await using var verifyDb = _dbFixture.CreateContext();
        Assert.Equal(0, await verifyDb.Bookings.CountAsync(b => b.ProductId == productId));
    }
}
