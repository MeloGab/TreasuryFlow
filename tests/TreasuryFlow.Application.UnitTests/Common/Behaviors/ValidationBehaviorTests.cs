using FluentValidation;
using MediatR;
using TreasuryFlow.Application.Common.Behaviors;

namespace TreasuryFlow.Application.UnitTests.Common.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WithoutValidators_ShouldExecuteNextBehavior()
    {
        var behavior = new ValidationBehavior<TestRequest, string>(
            []);

        var nextWasCalled = false;

        RequestHandlerDelegate<string> next =
            cancellationToken =>
            {
                nextWasCalled = true;

                return Task.FromResult("success");
            };

        var result = await behavior.Handle(
            new TestRequest("valid"),
            next,
            CancellationToken.None);

        Assert.True(nextWasCalled);
        Assert.Equal("success", result);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldExecuteNextBehavior()
    {
        var validator = CreateValidator();

        var behavior = new ValidationBehavior<TestRequest, string>(
            [validator]);

        var nextWasCalled = false;

        RequestHandlerDelegate<string> next =
            cancellationToken =>
            {
                nextWasCalled = true;

                return Task.FromResult("success");
            };

        var result = await behavior.Handle(
            new TestRequest("valid"),
            next,
            CancellationToken.None);

        Assert.True(nextWasCalled);
        Assert.Equal("success", result);
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ShouldThrowValidationException()
    {
        var validator = CreateValidator();

        var behavior = new ValidationBehavior<TestRequest, string>(
            [validator]);

        var nextWasCalled = false;

        RequestHandlerDelegate<string> next =
            cancellationToken =>
            {
                nextWasCalled = true;

                return Task.FromResult("success");
            };

        var action = () => behavior.Handle(
            new TestRequest(string.Empty),
            next,
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<ValidationException>(
            action);

        Assert.False(nextWasCalled);

        var error = Assert.Single(exception.Errors);

        Assert.Equal(
            nameof(TestRequest.Value),
            error.PropertyName);

        Assert.Equal(
            "Value is required.",
            error.ErrorMessage);
    }

    [Fact]
    public async Task Handle_WhenValidationIsCancelled_ShouldNotExecuteNextBehavior()
    {
        var validator = new InlineValidator<TestRequest>();

        validator
            .RuleFor(request => request.Value)
            .MustAsync(
                async (value, cancellationToken) =>
                {
                    await Task.Delay(
                        Timeout.InfiniteTimeSpan,
                        cancellationToken);

                    return true;
                });

        var behavior = new ValidationBehavior<TestRequest, string>(
            [validator]);

        var nextWasCalled = false;

        RequestHandlerDelegate<string> next =
            cancellationToken =>
            {
                nextWasCalled = true;

                return Task.FromResult("success");
            };

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        var action = () => behavior.Handle(
            new TestRequest("valid"),
            next,
            cancellationTokenSource.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            action);

        Assert.False(nextWasCalled);
    }

    private static InlineValidator<TestRequest> CreateValidator()
    {
        var validator = new InlineValidator<TestRequest>();

        validator
            .RuleFor(request => request.Value)
            .NotEmpty()
            .WithMessage("Value is required.");

        return validator;
    }

    private sealed record TestRequest(
        string Value);
}