using TreasuryFlow.Application.PaymentOrders.Commands.UpdatePaymentOrder;
using TreasuryFlow.Domain.PaymentOrders;

namespace TreasuryFlow.Application.UnitTests.PaymentOrders.Commands.UpdatePaymentOrder;

public sealed class UpdatePaymentOrderCommandValidatorTests
{
    private readonly UpdatePaymentOrderCommandValidator _validator =
        new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        var result = _validator.Validate(
            CreateValidCommand());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithEmptyId_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with
        {
            Id = Guid.Empty
        };

        var result = _validator.Validate(command);

        var error = Assert.Single(
            result.Errors,
            error => error.PropertyName ==
                nameof(UpdatePaymentOrderCommand.Id));

        Assert.Equal(
            "Payment order id is required.",
            error.ErrorMessage);
    }

    [Fact]
    public void Validate_WithInvalidDetails_ShouldHaveErrorsForAllFields()
    {
        var command = CreateValidCommand() with
        {
            Description = string.Empty,
            Amount = 0m,
            Currency = "JPY",
            Beneficiary = string.Empty
        };

        var result = _validator.Validate(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName ==
                nameof(UpdatePaymentOrderCommand.Description));
        Assert.Contains(
            result.Errors,
            error => error.PropertyName ==
                nameof(UpdatePaymentOrderCommand.Amount));
        Assert.Contains(
            result.Errors,
            error => error.PropertyName ==
                nameof(UpdatePaymentOrderCommand.Currency));
        Assert.Contains(
            result.Errors,
            error => error.PropertyName ==
                nameof(UpdatePaymentOrderCommand.Beneficiary));
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
                nameof(UpdatePaymentOrderCommand.Description));

        Assert.Equal(
            "Description cannot exceed 200 characters.",
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
                nameof(UpdatePaymentOrderCommand.Beneficiary));

        Assert.Equal(
            "Beneficiary cannot exceed 150 characters.",
            error.ErrorMessage);
    }

    [Theory]
    [InlineData(1.001)]
    [InlineData(10.999)]
    public void Validate_WithMoreThanTwoDecimalPlaces_ShouldHaveAmountError(
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
                nameof(UpdatePaymentOrderCommand.Amount));

        Assert.Equal(
            "Amount cannot have more than two decimal places.",
            error.ErrorMessage);
    }

    [Theory]
    [InlineData("brl")]
    [InlineData(" EUR ")]
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
                nameof(UpdatePaymentOrderCommand.Currency));
    }

    private static UpdatePaymentOrderCommand CreateValidCommand()
    {
        return new UpdatePaymentOrderCommand(
            Guid.NewGuid(),
            "Updated supplier payment",
            2500.75m,
            "EUR",
            "Updated Supplier Ltd.");
    }
}
