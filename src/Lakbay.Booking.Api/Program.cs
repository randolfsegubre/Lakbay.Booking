var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Phase 0: proves the service boots. Real endpoints (CQRS command/query
// handlers per ADR-0002, the atomic availability decrement per ADR-0011)
// land in Phase 4 — see Lakbay.Docs/docs/02_BUILD_PLAN.md.
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Lakbay.Booking" }))
    .WithName("HealthCheck");

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program;
