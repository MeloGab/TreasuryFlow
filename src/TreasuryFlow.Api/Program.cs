using Microsoft.EntityFrameworkCore;
using TreasuryFlow.Api.Common.ExceptionHandling;
using TreasuryFlow.Application;
using TreasuryFlow.Infrastructure;
using TreasuryFlow.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString(
        "TreasuryFlow");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'TreasuryFlow' is required.");
}

builder.Services.AddApplication();

builder.Services.AddInfrastructure(
    connectionString,
    builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Configuration.GetValue<bool>(
        "Database:ApplyMigrations"))
{
    await using var scope = app.Services.CreateAsyncScope();

    var dbContext = scope.ServiceProvider
        .GetRequiredService<TreasuryFlowDbContext>();

    await dbContext.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
