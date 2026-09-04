using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TreasuryFlow.Api.IntegrationTests.Health;

public sealed class HealthCheckTests(
    TreasuryFlowApiFactory factory)
    : IClassFixture<TreasuryFlowApiFactory>
{
    [Fact]
    public async Task GetLive_WhenApplicationIsRunning_ShouldReturnHealthy()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/health/live");

        var content = await response.Content
            .ReadAsStringAsync();

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            "Healthy",
            content);
    }

    [Fact]
    public async Task GetReady_WhenDatabaseIsAvailable_ShouldReturnHealthy()
    {
        await factory.ResetDatabaseAsync();

        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/health/ready");

        var content = await response.Content
            .ReadAsStringAsync();

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            "Healthy",
            content);
    }

    [Fact]
    public async Task Readiness_ShouldContainSqlServerCheck()
    {
        await factory.ResetDatabaseAsync();

        var healthCheckService = factory.Services
            .GetRequiredService<HealthCheckService>();

        var report = await healthCheckService.CheckHealthAsync(
            registration =>
                registration.Tags.Contains("ready"));

        var entry = Assert.Single(
            report.Entries);

        Assert.Equal(
            "sqlserver",
            entry.Key);

        Assert.Equal(
            HealthStatus.Healthy,
            entry.Value.Status);
    }
}
