using FluentValidation;
using TreasuryFlow.Domain.PaymentOrders;
using TreasuryFlow.Domain.PaymentOrders.ValueObjects;

namespace TreasuryFlow.Application.PaymentOrders.Commands.CreatePaymentOrder;

public sealed class CreatePaymentOrderCommandValidator
    : AbstractValidator<CreatePaymentOrderCommand>
{
    public CreatePaymentOrderCommandValidator()
    {
        RuleFor(command => command.Description)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Description is required.")
            .Must(description =>
                description.Trim().Length <=
                PaymentOrder.MaxDescriptionLength)
            .WithMessage(
                $"Description cannot exceed {PaymentOrder.MaxDescriptionLength} characters.");

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
                "Currency must be a valid three-letter code.")
            .Must(Money.IsSupportedCurrency)
            .WithMessage(
                "Currency is not supported.");

        RuleFor(command => command.Beneficiary)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Beneficiary is required.")
            .Must(beneficiary =>
                beneficiary.Trim().Length <=
                PaymentOrder.MaxBeneficiaryLength)
            .WithMessage(
                $"Beneficiary cannot exceed {PaymentOrder.MaxBeneficiaryLength} characters.");
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