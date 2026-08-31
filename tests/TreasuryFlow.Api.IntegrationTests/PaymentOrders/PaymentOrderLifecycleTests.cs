using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TreasuryFlow.Domain.PaymentOrders;
using TreasuryFlow.Infrastructure.Persistence;
using TreasuryFlow.Infrastructure.Persistence.Outbox;

namespace TreasuryFlow.Api.IntegrationTests.PaymentOrders;

public sealed class PaymentOrderLifecycleTests(
    TreasuryFlowApiFactory factory)
    : IClassFixture<TreasuryFlowApiFactory>
{
    [Fact]
    public async Task Submit_WhenDraft_ShouldPersistPendingPaymentOrder()
    {
        await factory.ResetDatabaseAsync();

        var paymentOrder = await PersistPaymentOrderAsync(
            PaymentOrderStatus.Draft);

        using var client = factory.CreateClient();

        var submitResponse = await client.PostAsync(
            $"/api/payment-orders/{paymentOrder.Id}/submit",
            content: null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            submitResponse.StatusCode);

        var persistedPaymentOrder =
            await LoadPaymentOrderAsync(paymentOrder.Id);

        Assert.Equal(
            PaymentOrderStatus.Pending,
            persistedPaymentOrder.Status);

        Assert.Null(
            persistedPaymentOrder.ProcessedAt);

        await AssertSingleSubmissionOutboxMessageAsync(
            paymentOrder.Id);
    }

    [Theory]
    [InlineData("start-processing")]
    [InlineData("complete")]
    [InlineData("fail")]
    public async Task InternalLifecycleEndpoint_ShouldNotBeExposed(
        string operation)
    {
        await factory.ResetDatabaseAsync();

        var paymentOrder = await PersistPaymentOrderAsync(
            PaymentOrderStatus.Draft);

        using var client = factory.CreateClient();

        var response = await client.PostAsync(
            $"/api/payment-orders/{paymentOrder.Id}/{operation}",
            content: null);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var persistedPaymentOrder =
            await LoadPaymentOrderAsync(paymentOrder.Id);

        Assert.Equal(
            PaymentOrderStatus.Draft,
            persistedPaymentOrder.Status);

        Assert.Null(
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

    private async Task AssertSingleSubmissionOutboxMessageAsync(
        Guid paymentOrderId)
    {
        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<TreasuryFlowDbContext>();

        var outboxMessage = await dbContext
            .Set<OutboxMessage>()
            .AsNoTracking()
            .SingleAsync();

        using var content = JsonDocument.Parse(
            outboxMessage.Content);

        Assert.Equal(
            paymentOrderId,
            content.RootElement
                .GetProperty("PaymentOrderId")
                .GetGuid());
    }
}
