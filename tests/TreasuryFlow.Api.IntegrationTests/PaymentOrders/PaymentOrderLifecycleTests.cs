using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TreasuryFlow.Domain.PaymentOrders;
using TreasuryFlow.Infrastructure.Persistence;

namespace TreasuryFlow.Api.IntegrationTests.PaymentOrders;

public sealed class PaymentOrderLifecycleTests(
    TreasuryFlowApiFactory factory)
    : IClassFixture<TreasuryFlowApiFactory>
{
    [Fact]
    public async Task CompleteFlow_ShouldPersistCompletedPaymentOrder()
    {
        await factory.ResetDatabaseAsync();

        var paymentOrder = await PersistPaymentOrderAsync(
            PaymentOrderStatus.Draft);

        using var client = factory.CreateClient();

        var submitResponse = await client.PostAsync(
            $"/api/payment-orders/{paymentOrder.Id}/submit",
            content: null);

        var startProcessingResponse = await client.PostAsync(
            $"/api/payment-orders/{paymentOrder.Id}/start-processing",
            content: null);

        var completeResponse = await client.PostAsync(
            $"/api/payment-orders/{paymentOrder.Id}/complete",
            content: null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            submitResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.NoContent,
            startProcessingResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.NoContent,
            completeResponse.StatusCode);

        var persistedPaymentOrder =
            await LoadPaymentOrderAsync(paymentOrder.Id);

        Assert.Equal(
            PaymentOrderStatus.Completed,
            persistedPaymentOrder.Status);

        Assert.NotNull(
            persistedPaymentOrder.ProcessedAt);
    }

    [Fact]
    public async Task FailFlow_ShouldPersistFailedPaymentOrder()
    {
        await factory.ResetDatabaseAsync();

        var paymentOrder = await PersistPaymentOrderAsync(
            PaymentOrderStatus.Processing);

        using var client = factory.CreateClient();

        var response = await client.PostAsync(
            $"/api/payment-orders/{paymentOrder.Id}/fail",
            content: null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        var persistedPaymentOrder =
            await LoadPaymentOrderAsync(paymentOrder.Id);

        Assert.Equal(
            PaymentOrderStatus.Failed,
            persistedPaymentOrder.Status);

        Assert.NotNull(
            persistedPaymentOrder.ProcessedAt);
    }

    [Theory]
    [InlineData(PaymentOrderStatus.Draft)]
    [InlineData(PaymentOrderStatus.Pending)]
    public async Task CancelFlow_WhenAllowed_ShouldPersistCancelledPaymentOrder(
        PaymentOrderStatus initialStatus)
    {
        await factory.ResetDatabaseAsync();

        var paymentOrder = await PersistPaymentOrderAsync(
            initialStatus);

        using var client = factory.CreateClient();

        var response = await client.PostAsync(
            $"/api/payment-orders/{paymentOrder.Id}/cancel",
            content: null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        var persistedPaymentOrder =
            await LoadPaymentOrderAsync(paymentOrder.Id);

        Assert.Equal(
            PaymentOrderStatus.Cancelled,
            persistedPaymentOrder.Status);
    }

    [Fact]
    public async Task Submit_WhenPaymentOrderDoesNotExist_ShouldReturnNotFound()
    {
        await factory.ResetDatabaseAsync();

        using var client = factory.CreateClient();

        var paymentOrderId = Guid.NewGuid();

        var response = await client.PostAsync(
            $"/api/payment-orders/{paymentOrderId}/submit",
            content: null);

        var problemDetails = await response.Content
            .ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.NotNull(problemDetails);

        Assert.Equal(
            "Payment order not found.",
            problemDetails.Title);

        Assert.Contains(
            paymentOrderId.ToString(),
            problemDetails.Detail);
    }

    [Fact]
    public async Task StartProcessing_WhenPaymentOrderIsDraft_ShouldReturnConflict()
    {
        await factory.ResetDatabaseAsync();

        var paymentOrder = await PersistPaymentOrderAsync(
            PaymentOrderStatus.Draft);

        using var client = factory.CreateClient();

        var response = await client.PostAsync(
            $"/api/payment-orders/{paymentOrder.Id}/start-processing",
            content: null);

        var problemDetails = await response.Content
            .ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        Assert.NotNull(problemDetails);

        Assert.Equal(
            "A domain rule was violated.",
            problemDetails.Title);

        Assert.Equal(
            "Only pending payment orders can start processing.",
            problemDetails.Detail);

        var persistedPaymentOrder =
            await LoadPaymentOrderAsync(paymentOrder.Id);

        Assert.Equal(
            PaymentOrderStatus.Draft,
            persistedPaymentOrder.Status);
    }

    private async Task<PaymentOrder> PersistPaymentOrderAsync(
        PaymentOrderStatus status)
    {
        var paymentOrder = PaymentOrder.Create(
            "Supplier payment",
            950.45m,
            "EUR",
            "European Supplier GmbH");

        switch (status)
        {
            case PaymentOrderStatus.Draft:
                break;

            case PaymentOrderStatus.Pending:
                paymentOrder.Submit();
                break;

            case PaymentOrderStatus.Processing:
                paymentOrder.Submit();
                paymentOrder.StartProcessing();
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "Unsupported initial status.");
        }

        paymentOrder.ClearDomainEvents();

        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<TreasuryFlowDbContext>();

        dbContext.PaymentOrders.Add(
            paymentOrder);

        await dbContext.SaveChangesAsync();

        return paymentOrder;
    }

    private async Task<PaymentOrder> LoadPaymentOrderAsync(
        Guid paymentOrderId)
    {
        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<TreasuryFlowDbContext>();

        return await dbContext.PaymentOrders
            .AsNoTracking()
            .SingleAsync(
                paymentOrder =>
                    paymentOrder.Id == paymentOrderId);
    }
}
