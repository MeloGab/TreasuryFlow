using TreasuryFlow.Domain.Common.Exceptions;
using TreasuryFlow.Domain.PaymentOrders;

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
        var paymentOrder = PaymentOrder.Create(
            description: "Pagamento fornecedor",
            amount: 15000,
            currency: "BRL",
            beneficiary: "Fornecedor XPTO");

        Assert.NotEqual(Guid.Empty, paymentOrder.Id);
        Assert.Equal("Pagamento fornecedor", paymentOrder.Description);
        Assert.Equal(15000, paymentOrder.Amount);
        Assert.Equal("BRL", paymentOrder.Currency);
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
            15000,
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
            15000,
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
            15000,
            "BRL",
            beneficiary);

        Assert.Throws<DomainException>(action);
    }

}