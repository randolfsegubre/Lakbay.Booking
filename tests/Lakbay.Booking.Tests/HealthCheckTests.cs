using Microsoft.AspNetCore.Mvc.Testing;

namespace Lakbay.Booking.Tests;

/// <summary>
/// Phase 0's only real test: the service actually boots. Real coverage
/// (CQRS handlers, the atomic availability decrement) arrives in Phase 4 —
/// see Lakbay.Docs/docs/02_BUILD_PLAN.md.
/// </summary>
public class HealthCheckTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Health_endpoint_returns_ok()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.True(response.IsSuccessStatusCode);
    }
}
