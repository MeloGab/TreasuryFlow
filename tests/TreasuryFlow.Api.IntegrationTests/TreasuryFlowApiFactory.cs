using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TreasuryFlow.Infrastructure.Persistence;

namespace TreasuryFlow.Api.IntegrationTests;

public sealed class TreasuryFlowApiFactory
    : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new(
        "Data Source=:memory:");

    public TreasuryFlowApiFactory()
    {
        _connection.Open();
    }

    protected override IHost CreateHost(
        IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(
            configurationBuilder =>
                configurationBuilder.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:TreasuryFlow"] =
                            "Server=unused;Database=TreasuryFlowTests"
                    }));

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment(
            "Testing");

        builder.ConfigureServices(
            services =>
            {
                services.RemoveAll<
                    TreasuryFlowDbContext>();

                services.RemoveAll<
                    DbContextOptions<TreasuryFlowDbContext>>();

                services.RemoveAll<
                    IDbContextOptionsConfiguration<
                        TreasuryFlowDbContext>>();

                services.AddDbContext<TreasuryFlowDbContext>(
                    options =>
                        options.UseSqlite(
                            _connection));
            });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<TreasuryFlowDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    protected override void Dispose(
        bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
