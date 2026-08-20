using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TreasuryFlow.Application.Common.Behaviors;

namespace TreasuryFlow.Application;

public static class DependencyInjection
{
    private static readonly Assembly ApplicationAssembly =
        typeof(DependencyInjection).Assembly;

    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(
                ApplicationAssembly);

            configuration.AddOpenBehavior(
                typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(
            ApplicationAssembly);

        return services;
    }
}