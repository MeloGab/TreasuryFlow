using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TreasuryFlow.Api.Contracts.PaymentOrders;
using TreasuryFlow.Domain.PaymentOrders;
using TreasuryFlow.Infrastructure.Persistence;

namespace TreasuryFlow.Api.IntegrationTests.PaymentOrders;

public sealed class CreatePaymentOrderTests(
    TreasuryFlowApiFactory factory)
    : IClassFixture<TreasuryFlowApiFactory>
{
    [Fact]
    public async Task PostAsync_WithValidRequest_ShouldCreatePaymentOrder()
    {
        await factory.ResetDatabaseAsync();

        using var client = factory.CreateClient();

        var request = new CreatePaymentOrderRequest(
            "Supplier invoice",
            1250.75m,
            "brl",
            "Acme Ltd.");

        var httpResponse = await client.PostAsJsonAsync(
            "/api/payment-orders",
            request);

        var response = await httpResponse.Content
            .ReadFromJsonAsync<CreatePaymentOrderResponse>();

        Assert.Equal(
            HttpStatusCode.Created,
            httpResponse.StatusCode);

        Assert.NotNull(response);

        Assert.NotEqual(
            Guid.Empty,
            response.Id);

        Assert.Equal(
            new Uri(
                client.BaseAddress!,
                $"/api/payment-orders/{response.Id}"),
            httpResponse.Headers.Location);

        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<TreasuryFlowDbContext>();

        var paymentOrder = await dbContext.PaymentOrders
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(
            response.Id,
            paymentOrder.Id);

        Assert.Equal(
            "Supplier invoice",
            paymentOrder.Description);

        Assert.Equal(
            1250.75m,
            paymentOrder.Amount.Value);

        Assert.Equal(
            "BRL",
            paymentOrder.Amount.Currency);

        Assert.Equal(
            "Acme Ltd.",
            paymentOrder.Beneficiary);

        Assert.Equal(
            PaymentOrderStatus.Draft,
            paymentOrder.Status);
    }

    [Fact]
    public async Task PostAsync_WithInvalidRequest_ShouldReturnValidationProblem()
    {
        await factory.ResetDatabaseAsync();

        using var client = factory.CreateClient();

        var request = new CreatePaymentOrderRequest(
            string.Empty,
            0m,
            "JPY",
            string.Empty);

        var httpResponse = await client.PostAsJsonAsync(
            "/api/payment-orders",
            request);

        var problemDetails = await httpResponse.Content
            .ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.Equal(
            HttpStatusCode.BadRequest,
            httpResponse.StatusCode);

        Assert.NotNull(problemDetails);

        Assert.Equal(
            "One or more validation errors occurred.",
            problemDetails.Title);

        Assert.Contains(
            "Description",
            problemDetails.Errors.Keys);

        Assert.Contains(
            "Amount",
            problemDetails.Errors.Keys);

        Assert.Contains(
            "Currency",
            problemDetails.Errors.Keys);

        Assert.Contains(
            "Beneficiary",
            problemDetails.Errors.Keys);

        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<TreasuryFlowDbContext>();

        Assert.Empty(
            await dbContext.PaymentOrders
                .AsNoTracking()
                .ToListAsync());
    }
}
