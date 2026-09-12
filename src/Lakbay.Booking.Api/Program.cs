using System.Text.Json.Serialization;
using Lakbay.Booking.Api.Api;
using Lakbay.Booking.Api.Application.Commands;
using Lakbay.Booking.Api.Application.Queries;
using Lakbay.Booking.Api.Grpc;
using Lakbay.Booking.Api.Infrastructure;
using Lakbay.Booking.Api.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// ADR-0027: Agent Channel confirm-booking call. Additive alongside the
// existing REST endpoints below — Lakbay.Web's online checkout keeps
// using REST; only Lakbay.AgentOps calls this.
builder.Services.AddGrpc();

// So a curl body can send "channel": "Agent" instead of a raw enum index.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// ADR-0005: SQL Server locally (LocalDB by default here — see
// appsettings.json's "BookingDb" connection string; swapping to the
// platform's shared Docker SQL Server / lakbayBookingDb, or to Azure SQL
// in a real environment, is a connection-string-only change), never
// Azure SQL Database for local dev.
builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BookingDb")));

// ADR-0002: MediatR — separate command/query handlers, never a shared
// "BookingService" class with both read and write methods on it.
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

// Strategy pattern (03_ARCHITECTURE_AND_PATTERNS_GUIDE.md): the Online
// channel depends on the interface, not on PayMongo directly. Real
// implementation is still blocked on a sandbox account (ADR-0026).
builder.Services.AddScoped<IPaymentGateway, PayMongoPaymentGateway>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Phase 4/7 exit criteria (ADR-0002, ADR-0026): prove the DI graph
// actually resolves and the database is actually reachable, not just
// that the classes compile — migrate + seed on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    await db.Database.MigrateAsync();
    await AvailabilitySeeder.SeedAsync(db);
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Lakbay.Booking" }))
    .WithName("HealthCheck");

// ADR-0027: Agent Channel only, reached via Lakbay.AgentOps's gRPC client
// (Grpc.Net.Client), never by a browser or Lakbay.Web directly.
app.MapGrpcService<BookingConfirmGrpcService>();

app.MapPost("/api/bookings/confirm", async (ConfirmBookingRequest request, IMediator mediator, CancellationToken cancellationToken) =>
{
    var command = new ConfirmBookingCommand(request.ProductId, request.DateSlot, request.CustomerId, request.Channel);
    var result = await mediator.Send(command, cancellationToken);

    return result.Status switch
    {
        ConfirmBookingStatus.Confirmed => Results.Ok(new
        {
            bookingId = result.BookingId,
            paymentStatus = result.PaymentStatus!.Value.ToString(),
            message = result.Message
        }),
        ConfirmBookingStatus.NotAvailable => Results.Conflict(new { message = result.Message }),
        ConfirmBookingStatus.PaymentGatewayNotImplemented => Results.Json(
            new { message = result.Message },
            statusCode: StatusCodes.Status501NotImplemented),
        _ => Results.Problem("Unexpected confirm-booking result.")
    };
})
.WithName("ConfirmBooking");

app.MapGet("/api/bookings/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
{
    var booking = await mediator.Send(new GetBookingByIdQuery(id), cancellationToken);
    return booking is null ? Results.NotFound() : Results.Ok(booking);
})
.WithName("GetBookingById");

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program;
