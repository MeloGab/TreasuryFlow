using TreasuryFlow.Domain.Common.Exceptions;
using TreasuryFlow.Domain.PaymentOrders;
using TreasuryFlow.Domain.PaymentOrders.Events;

namespace TreasuryFlow.Domain.UnitTests.PaymentOrders;

public class PaymentOrderTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void Create_WithNonPositiveAmount_ShouldThrowDomainException(
        decimal amount)
    {
        var action = () => PaymentOrder.Create(
            description: "Pagamento fornecedor",
            amount: amount,
            currency: "BRL",
            beneficiary: "Fornecedor XPTO");

        Assert.Throws<DomainException>(action);
    }

    [Fact]
    public void Create_WithValidData_ShouldCreateDraftPaymentOrder()
    {
        var paymentOrder = CreateValidPaymentOrder();

        Assert.NotEqual(Guid.Empty, paymentOrder.Id);
        Assert.Equal("Pagamento fornecedor", paymentOrder.Description);
        Assert.Equal(15000m, paymentOrder.Amount.Value);
        Assert.Equal("BRL", paymentOrder.Amount.Currency);
        Assert.Equal("Fornecedor XPTO", paymentOrder.Beneficiary);
        Assert.Equal(PaymentOrderStatus.Draft, paymentOrder.Status);
        Assert.NotEqual(default, paymentOrder.CreatedAt);
        Assert.Null(paymentOrder.ProcessedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithoutDescription_ShouldThrowDomainException(
        string description)
    {
        var action = () => PaymentOrder.Create(
            description,
            15000m,
            "BRL",
            "Fornecedor XPTO");

        Assert.Throws<DomainException>(action);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithoutCurrency_ShouldThrowDomainException(
        string currency)
    {
        var action = () => PaymentOrder.Create(
            "Pagamento fornecedor",
            15000m,
            currency,
            "Fornecedor XPTO");

        Assert.Throws<DomainException>(action);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithoutBeneficiary_ShouldThrowDomainException(
        string beneficiary)
    {
        var action = () => PaymentOrder.Create(
            "Pagamento fornecedor",
            15000m,
            "BRL",
            beneficiary);

        Assert.Throws<DomainException>(action);
    }

    [Fact]
    public void Submit_WhenDraft_ShouldChangeStatusToPending()
    {
        var paymentOrder = CreateValidPaymentOrder();

        paymentOrder.Submit();

        Assert.Equal(PaymentOrderStatus.Pending, paymentOrder.Status);
    }

    [Fact]
    public void Submit_WhenNotDraft_ShouldThrowDomainException()
    {
        var paymentOrder = CreateValidPaymentOrder();

        paymentOrder.Submit();

        var action = () => paymentOrder.Submit();

        Assert.Throws<DomainException>(action);
    }

    [Fact]
    public void StartProcessing_WhenPending_ShouldChangeStatusToProcessing()
    {
        var paymentOrder = CreateValidPaymentOrder();

        paymentOrder.Submit();
        paymentOrder.StartProcessing();

        Assert.Equal(PaymentOrderStatus.Processing, paymentOrder.Status);
    }

    [Fact]
    public void StartProcessing_WhenNotPending_ShouldThrowDomainException()
    {
        var paymentOrder = CreateValidPaymentOrder();

        var action = () => paymentOrder.StartProcessing();

        Assert.Throws<DomainException>(action);
    }

    [Fact]
    public void Complete_WhenProcessing_ShouldChangeStatusToCompleted()
    {
        var paymentOrder = CreateProcessingPaymentOrder();

        paymentOrder.Complete();

        Assert.Equal(PaymentOrderStatus.Completed, paymentOrder.Status);
        Assert.NotNull(paymentOrder.ProcessedAt);
    }

    [Fact]
    public void Complete_WhenNotProcessing_ShouldThrowDomainException()
    {
        var paymentOrder = CreateValidPaymentOrder();

        var action = () => paymentOrder.Complete();

        Assert.Throws<DomainException>(action);
    }

    [Fact]
    public void Fail_WhenProcessing_ShouldChangeStatusToFailed()
    {
        var paymentOrder = CreateProcessingPaymentOrder();

        paymentOrder.Fail();

        Assert.Equal(PaymentOrderStatus.Failed, paymentOrder.Status);
        Assert.NotNull(paymentOrder.ProcessedAt);
    }

    [Fact]
    public void Fail_WhenNotProcessing_ShouldThrowDomainException()
    {
        var paymentOrder = CreateValidPaymentOrder();

        var action = () => paymentOrder.Fail();

        Assert.Throws<DomainException>(action);
    }

    [Fact]
    public void Cancel_WhenDraft_ShouldChangeStatusToCancelled()
    {
        var paymentOrder = CreateValidPaymentOrder();

        paymentOrder.Cancel();

        Assert.Equal(PaymentOrderStatus.Cancelled, paymentOrder.Status);
    }

    [Fact]
    public void Cancel_WhenPending_ShouldChangeStatusToCancelled()
    {
        var paymentOrder = CreateValidPaymentOrder();

        paymentOrder.Submit();
        paymentOrder.Cancel();

        Assert.Equal(PaymentOrderStatus.Cancelled, paymentOrder.Status);
    }

    [Fact]
    public void Cancel_WhenProcessing_ShouldThrowDomainException()
    {
        var paymentOrder = CreateProcessingPaymentOrder();

        var action = () => paymentOrder.Cancel();

        Assert.Throws<DomainException>(action);
    }

    [Fact]
    public void Cancel_WhenCompleted_ShouldThrowDomainException()
    {
        var paymentOrder = CreateProcessingPaymentOrder();

        paymentOrder.Complete();

        var action = () => paymentOrder.Cancel();

        Assert.Throws<DomainException>(action);
    }

    [Fact]
    public void Submit_WhenDraft_ShouldRaisePaymentOrderSubmittedDomainEvent()
    {
        var paymentOrder = CreateValidPaymentOrder();

        paymentOrder.Submit();

        var domainEvent = Assert.Single(paymentOrder.DomainEvents);

        var submittedEvent =
            Assert.IsType<PaymentOrderSubmittedDomainEvent>(domainEvent);

        Assert.Equal(paymentOrder.Id, submittedEvent.PaymentOrderId);
        Assert.Equal(paymentOrder.Amount.Value, submittedEvent.Amount);
        Assert.Equal(paymentOrder.Amount.Currency, submittedEvent.Currency);
        Assert.NotEqual(default, submittedEvent.OccurredAt);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllDomainEvents()
    {
        var paymentOrder = CreateValidPaymentOrder();

        paymentOrder.Submit();

        Assert.NotEmpty(paymentOrder.DomainEvents);

        paymentOrder.ClearDomainEvents();

        Assert.Empty(paymentOrder.DomainEvents);
    }

    private static PaymentOrder CreateValidPaymentOrder()
    {
        return PaymentOrder.Create(
            description: "Pagamento fornecedor",
            amount: 15000m,
            currency: "BRL",
            beneficiary: "Fornecedor XPTO");
    }

    private static PaymentOrder CreateProcessingPaymentOrder()
    {
        var paymentOrder = CreateValidPaymentOrder();

        paymentOrder.Submit();
        paymentOrder.StartProcessing();

        return paymentOrder;
    }
}