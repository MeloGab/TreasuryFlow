using TreasuryFlow.Infrastructure;

var builder = Host.CreateApplicationBuilder(
    args);

var connectionString = builder.Configuration
    .GetConnectionString(
        "TreasuryFlow");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'TreasuryFlow' is required.");
}

builder.Services.AddWorkerInfrastructure(
    connectionString,
    builder.Configuration);

var host = builder.Build();

await host.RunAsync();
