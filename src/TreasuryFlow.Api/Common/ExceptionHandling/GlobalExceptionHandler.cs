using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

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
