using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using TreasuryFlow.Application.Common.Exceptions;
using TreasuryFlow.Domain.Common.Exceptions;

namespace TreasuryFlow.Api.Common.ExceptionHandling;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken _)
    {
        if (exception is ValidationException validationException)
        {
            await WriteValidationProblemAsync(
                httpContext,
                validationException);

            return true;
        }

        if (exception is PaymentOrderNotFoundException notFoundException)
        {
            await Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Payment order not found.",
                    detail: notFoundException.Message)
                .ExecuteAsync(httpContext);

            return true;
        }

        if (exception is DomainException domainException)
        {
            await Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "A domain rule was violated.",
                    detail: domainException.Message)
                .ExecuteAsync(httpContext);

            return true;
        }

        logger.LogError(
            exception,
            "An unhandled exception occurred while processing the request.");

        await Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred.")
            .ExecuteAsync(httpContext);

        return true;
    }

    private static async Task WriteValidationProblemAsync(
        HttpContext httpContext,
        ValidationException validationException)
    {
        var errors = validationException.Errors
            .GroupBy(
                failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(
                        failure => failure.ErrorMessage)
                    .Distinct()
                    .ToArray());

        var validationProblem = Results.ValidationProblem(
            errors,
            statusCode: StatusCodes.Status400BadRequest,
            title: "One or more validation errors occurred.");

        await validationProblem.ExecuteAsync(
            httpContext);
    }
}
