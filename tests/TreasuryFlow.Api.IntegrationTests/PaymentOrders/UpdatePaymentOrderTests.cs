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

public sealed class UpdatePaymentOrderTests(
    TreasuryFlowApiFactory factory)
    : IClassFixture<TreasuryFlowApiFactory>
{
    [Fact]
    public async Task PutAsync_WhenDraft_ShouldUpdatePaymentOrder()
    {
        await factory.ResetDatabaseAsync();
        var paymentOrder = await PersistPaymentOrderAsync(
            submit: false);
        using var client = factory.CreateClient();
        var request = CreateValidRequest();

        var response = await client.PutAsJsonAsync(
            $"/api/payment-orders/{paymentOrder.Id}",
            request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        var persistedPaymentOrder =
            await LoadPaymentOrderAsync(paymentOrder.Id);

        Assert.Equal(
            request.Description,
            persistedPaymentOrder.Description);
        Assert.Equal(
            request.Amount,
            persistedPaymentOrder.Amount.Value);
        Assert.Equal(
            "EUR",
            persistedPaymentOrder.Amount.Currency);
        Assert.Equal(
            request.Beneficiary,
            persistedPaymentOrder.Beneficiary);
        Assert.Equal(
            PaymentOrderStatus.Draft,
            persistedPaymentOrder.Status);
        Assert.Equal(
            paymentOrder.CreatedAt,
            persistedPaymentOrder.CreatedAt);
        Assert.Null(persistedPaymentOrder.ProcessedAt);
    }

    [Fact]
    public async Task PutAsync_WhenNotDraft_ShouldReturnConflictAndPreserveData()
    {
        await factory.ResetDatabaseAsync();
        var paymentOrder = await PersistPaymentOrderAsync(
            submit: true);
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/payment-orders/{paymentOrder.Id}",
            CreateValidRequest());

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
            "Only draft payment orders can be updated.",
            problemDetails.Detail);

        var persistedPaymentOrder =
            await LoadPaymentOrderAsync(paymentOrder.Id);

        Assert.Equal(
            "Supplier payment",
            persistedPaymentOrder.Description);
        Assert.Equal(
            1250.50m,
            persistedPaymentOrder.Amount.Value);
        Assert.Equal(
            "BRL",
            persistedPaymentOrder.Amount.Currency);
        Assert.Equal(
            "Supplier Ltd.",
            persistedPaymentOrder.Beneficiary);
        Assert.Equal(
            PaymentOrderStatus.Pending,
            persistedPaymentOrder.Status);
    }

    [Fact]
    public async Task PutAsync_WithInvalidRequest_ShouldReturnValidationProblem()
    {
        await factory.ResetDatabaseAsync();
        var paymentOrder = await PersistPaymentOrderAsync(
            submit: false);
        using var client = factory.CreateClient();
        var request = new UpdatePaymentOrderRequest(
            string.Empty,
            0m,
            "JPY",
            string.Empty);

        var response = await client.PutAsJsonAsync(
            $"/api/payment-orders/{paymentOrder.Id}",
            request);

        var problemDetails = await response.Content
            .ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
        Assert.NotNull(problemDetails);
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

        var persistedPaymentOrder =
            await LoadPaymentOrderAsync(paymentOrder.Id);

        Assert.Equal(
            "Supplier payment",
            persistedPaymentOrder.Description);
        Assert.Equal(
            1250.50m,
            persistedPaymentOrder.Amount.Value);
        Assert.Equal(
            "BRL",
            persistedPaymentOrder.Amount.Currency);
        Assert.Equal(
            "Supplier Ltd.",
            persistedPaymentOrder.Beneficiary);
    }

    [Fact]
    public async Task PutAsync_WhenPaymentOrderDoesNotExist_ShouldReturnNotFound()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();
        var paymentOrderId = Guid.NewGuid();

        var response = await client.PutAsJsonAsync(
            $"/api/payment-orders/{paymentOrderId}",
            CreateValidRequest());

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

    private static UpdatePaymentOrderRequest CreateValidRequest()
    {
        return new UpdatePaymentOrderRequest(
            "Updated supplier payment",
            2500.75m,
            " eur ",
            "Updated Supplier Ltd.");
    }

    private async Task<PaymentOrder> PersistPaymentOrderAsync(
        bool submit)
    {
        var paymentOrder = PaymentOrder.Create(
            "Supplier payment",
            1250.50m,
            "BRL",
            "Supplier Ltd.");

        if (submit)
        {
            paymentOrder.Submit();
            paymentOrder.ClearDomainEvents();
        }

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<TreasuryFlowDbContext>();

        dbContext.PaymentOrders.Add(paymentOrder);
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
