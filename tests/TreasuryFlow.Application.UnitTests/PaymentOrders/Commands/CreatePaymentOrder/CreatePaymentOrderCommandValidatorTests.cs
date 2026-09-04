namespace TreasuryFlow.Application.UnitTests.PaymentOrders.Commands.CreatePaymentOrder;

using TreasuryFlow.Application.PaymentOrders.Commands.CreatePaymentOrder;
using TreasuryFlow.Domain.PaymentOrders;

public class CreatePaymentOrderCommandValidatorTests
{
    private readonly CreatePaymentOrderCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        var command = CreateValidCommand();

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WithoutDescription_ShouldHaveValidationError(
        string? description)
    {
        var command = CreateValidCommand() with
        {
            Description = description!
        };

        var result = _validator.Validate(command);

        var error = Assert.Single(
            result.Errors,
            error => error.PropertyName ==
                nameof(CreatePaymentOrderCommand.Description));

        Assert.Equal(
            "Description is required.",
            error.ErrorMessage);
    }

    [Fact]
    public void Validate_WithDescriptionExceedingMaximumLength_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with
        {
            Description = new string(
                'a',
                PaymentOrder.MaxDescriptionLength + 1)
        };

        var result = _validator.Validate(command);

        var error = Assert.Single(
            result.Errors,
            error => error.PropertyName ==
                nameof(CreatePaymentOrderCommand.Description));

        Assert.Equal(
            "Description cannot exceed 200 characters.",
            error.ErrorMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void Validate_WithNonPositiveAmount_ShouldHaveValidationError(
        decimal amount)
    {
        var command = CreateValidCommand() with
        {
            Amount = amount
        };

        var result = _validator.Validate(command);

        var error = Assert.Single(
            result.Errors,
            error => error.PropertyName ==
                nameof(CreatePaymentOrderCommand.Amount));

        Assert.Equal(
            "Amount must be greater than zero.",
            error.ErrorMessage);
    }

    [Theory]
    [InlineData(1.001)]
    [InlineData(10.999)]
    [InlineData(15000.123)]
    public void Validate_WithMoreThanTwoDecimalPlaces_ShouldHaveValidationError(
        decimal amount)
    {
        var command = CreateValidCommand() with
        {
            Amount = amount
        };

        var result = _validator.Validate(command);

        var error = Assert.Single(
            result.Errors,
            error => error.PropertyName ==
                nameof(CreatePaymentOrderCommand.Amount));

        Assert.Equal(
            "Amount cannot have more than two decimal places.",
            error.ErrorMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WithoutCurrency_ShouldHaveValidationError(
        string? currency)
    {
        var command = CreateValidCommand() with
        {
            Currency = currency!
        };

        var result = _validator.Validate(command);

        var error = Assert.Single(
            result.Errors,
            error => error.PropertyName ==
                nameof(CreatePaymentOrderCommand.Currency));

        Assert.Equal(
            "Currency is required.",
            error.ErrorMessage);
    }

    [Theory]
    [InlineData("BR")]
    [InlineData("BRLL")]
    [InlineData("123")]
    [InlineData("@@@")]
    public void Validate_WithInvalidCurrencyFormat_ShouldHaveValidationError(
        string currency)
    {
        var command = CreateValidCommand() with
        {
            Currency = currency
        };

        var result = _validator.Validate(command);

        var error = Assert.Single(
            result.Errors,
            error => error.PropertyName ==
                nameof(CreatePaymentOrderCommand.Currency));

        Assert.Equal(
            "Currency must be a valid three-letter code.",
            error.ErrorMessage);
    }

    [Theory]
    [InlineData("brl")]
    [InlineData(" BRL ")]
    public void Validate_WithNormalizableCurrency_ShouldNotHaveCurrencyError(
        string currency)
    {
        var command = CreateValidCommand() with
        {
            Currency = currency
        };

        var result = _validator.Validate(command);

        Assert.DoesNotContain(
            result.Errors,
            error => error.PropertyName ==
                nameof(CreatePaymentOrderCommand.Currency));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WithoutBeneficiary_ShouldHaveValidationError(
        string? beneficiary)
    {
        var command = CreateValidCommand() with
        {
            Beneficiary = beneficiary!
        };

        var result = _validator.Validate(command);

        var error = Assert.Single(
            result.Errors,
            error => error.PropertyName ==
                nameof(CreatePaymentOrderCommand.Beneficiary));

        Assert.Equal(
            "Beneficiary is required.",
            error.ErrorMessage);
    }

    [Fact]
    public void Validate_WithBeneficiaryExceedingMaximumLength_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with
        {
            Beneficiary = new string(
                'a',
                PaymentOrder.MaxBeneficiaryLength + 1)
        };

        var result = _validator.Validate(command);

        var error = Assert.Single(
            result.Errors,
            error => error.PropertyName ==
                nameof(CreatePaymentOrderCommand.Beneficiary));

        Assert.Equal(
            "Beneficiary cannot exceed 150 characters.",
            error.ErrorMessage);
    }

    [Theory]
    [InlineData("JPY")]
    [InlineData("GBP")]
    public void Validate_WithUnsupportedCurrency_ShouldHaveValidationError(
        string currency)
    {
        var command = CreateValidCommand() with
        {
            Currency = currency
        };

        var result = _validator.Validate(command);

        var error = Assert.Single(
            result.Errors,
            error => error.PropertyName ==
                nameof(CreatePaymentOrderCommand.Currency));

        Assert.Equal(
            "Currency is not supported.",
            error.ErrorMessage);
    }
    private static CreatePaymentOrderCommand CreateValidCommand()
    {
        return new CreatePaymentOrderCommand(
            Description: "Pagamento fornecedor",
            Amount: 15000m,
            Currency: "BRL",
            Beneficiary: "Fornecedor XPTO");
    }
}