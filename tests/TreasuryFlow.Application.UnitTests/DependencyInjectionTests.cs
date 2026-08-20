using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TreasuryFlow.Application.Common.Behaviors;
using TreasuryFlow.Application.PaymentOrders.Commands.CreatePaymentOrder;

namespace TreasuryFlow.Application.UnitTests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_ShouldRegisterMediatRAndCommandHandler()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IMediator));

        Assert.Contains(
            services,
            service => service.ServiceType ==
                typeof(
                    IRequestHandler<
                        CreatePaymentOrderCommand,
                        Guid>));
    }

    [Fact]
    public void AddApplication_ShouldRegisterCommandValidator()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        Assert.Contains(
            services,
            service => service.ServiceType ==
                typeof(
                    IValidator<
                        CreatePaymentOrderCommand>));
    }

    [Fact]
    public void AddApplication_ShouldRegisterValidationBehavior()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        Assert.Contains(
            services,
            service =>
                service.ServiceType ==
                    typeof(IPipelineBehavior<,>) &&
                service.ImplementationType ==
                    typeof(ValidationBehavior<,>));
    }

    [Fact]
    public void AddApplication_ShouldReturnSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddApplication();

        Assert.Same(services, result);
    }
}