using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TreasuryFlow.Api.Contracts.PaymentOrders;
using TreasuryFlow.Domain.PaymentOrders;
using TreasuryFlow.Infrastructure.Persistence;

namespace TreasuryFlow.Api.IntegrationTests.PaymentOrders;

public sealed class GetPaymentOrderByIdTests(
    TreasuryFlowApiFactory factory)
    : IClassFixture<TreasuryFlowApiFactory>
{
    [Fact]
    public async Task GetAsync_WhenPaymentOrderExists_ShouldReturnPaymentOrder()
    {
        await factory.ResetDatabaseAsync();

        var paymentOrder = PaymentOrder.Create(
            "International supplier payment",
            990.30m,
            "eur",
            "European Supplier GmbH");

        paymentOrder.Submit();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<TreasuryFlowDbContext>();

            dbContext.PaymentOrders.Add(
                paymentOrder);

            await dbContext.SaveChangesAsync();
        }

        using var client = factory.CreateClient();

        var httpResponse = await client.GetAsync(
            $"/api/payment-orders/{paymentOrder.Id}");

        var response = await httpResponse.Content
            .ReadFromJsonAsync<GetPaymentOrderByIdResponse>();

        Assert.Equal(
            HttpStatusCode.OK,
            httpResponse.StatusCode);

        Assert.NotNull(response);
        Assert.Equal(paymentOrder.Id, response.Id);
        Assert.Equal(paymentOrder.Description, response.Description);
        Assert.Equal(paymentOrder.Amount.Value, response.Amount);
        Assert.Equal(paymentOrder.Amount.Currency, response.Currency);
        Assert.Equal(paymentOrder.Beneficiary, response.Beneficiary);
        Assert.Equal(paymentOrder.Status.ToString(), response.Status);
        Assert.Equal(paymentOrder.CreatedAt, response.CreatedAt);
        Assert.Equal(paymentOrder.ProcessedAt, response.ProcessedAt);
    }

    [Fact]
    public async Task GetAsync_WhenPaymentOrderDoesNotExist_ShouldReturnNotFound()
    {
        await factory.ResetDatabaseAsync();

        using var client = factory.CreateClient();

        var paymentOrderId = Guid.NewGuid();

        var httpResponse = await client.GetAsync(
            $"/api/payment-orders/{paymentOrderId}");

        var problemDetails = await httpResponse.Content
            .ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(
            HttpStatusCode.NotFound,
            httpResponse.StatusCode);

        Assert.NotNull(problemDetails);

        Assert.Equal(
            "Payment order not found.",
            problemDetails.Title);

        Assert.Equal(
            StatusCodes.Status404NotFound,
            problemDetails.Status);
    }
}
