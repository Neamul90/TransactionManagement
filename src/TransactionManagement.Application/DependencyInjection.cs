using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TransactionManagement.Application.Behaviors;
using TransactionManagement.Application.Transactions.Commands.Shared;

namespace TransactionManagement.Application;

/// <summary>
/// Composition root for the Application layer. The Web project calls this; it never registers
/// individual handlers or validators itself.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(applicationAssembly);

            // Order matters: logging wraps performance, which wraps validation,
            // so a request rejected by validation is still logged as handled.
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(PerformanceBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(applicationAssembly, includeInternalTypes: false);

        services.AddScoped<TransactionReferenceChecker>();

        return services;
    }
}
