using FluentValidation;

namespace TreasuryFlow.Application.PaymentOrders.Commands.CreatePaymentOrder;

public sealed class CreatePaymentOrderCommandValidator
    : AbstractValidator<CreatePaymentOrderCommand>
{
    public CreatePaymentOrderCommandValidator()
    {
        RuleFor(command => command.Description)
            .NotEmpty()
            .WithMessage("Description is required.");

        RuleFor(command => command.Amount)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero.")
            .Must(HaveAtMostTwoDecimalPlaces)
            .WithMessage(
                "Amount cannot have more than two decimal places.");

        RuleFor(command => command.Currency)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Currency is required.")
            .Must(BeValidThreeLetterCurrencyCode)
            .WithMessage(
                "Currency must be a valid three-letter code.");

        RuleFor(command => command.Beneficiary)
            .NotEmpty()
            .WithMessage("Beneficiary is required.");
    }

    private static bool HaveAtMostTwoDecimalPlaces(
        decimal amount)
    {
        return decimal.Round(amount, 2) == amount;
    }

    private static bool BeValidThreeLetterCurrencyCode(
        string currency)
    {
        var normalizedCurrency = currency.Trim();

        return normalizedCurrency.Length == 3 &&
            normalizedCurrency.All(char.IsLetter);
    }
}