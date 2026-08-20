using FluentValidation;
using MediatR;

namespace TreasuryFlow.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var registeredValidators = validators.ToArray();

        if (registeredValidators.Length == 0)
        {
            return await next(cancellationToken);
        }

        var validationContext =
            new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            registeredValidators.Select(
                validator => validator.ValidateAsync(
                    validationContext,
                    cancellationToken)));

        var validationFailures = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToArray();

        if (validationFailures.Length > 0)
        {
            throw new ValidationException(
                validationFailures);
        }

        return await next(cancellationToken);
    }
}