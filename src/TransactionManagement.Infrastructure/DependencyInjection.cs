using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TransactionManagement.Application.Abstractions.Persistence;
using TransactionManagement.Application.Abstractions.Reporting;
using TransactionManagement.Application.Abstractions.Services;
using TransactionManagement.Infrastructure.Persistence;
using TransactionManagement.Infrastructure.Persistence.Interceptors;
using TransactionManagement.Infrastructure.Persistence.Repositories;
using TransactionManagement.Infrastructure.Reporting;
using TransactionManagement.Infrastructure.Services;

namespace TransactionManagement.Infrastructure;

/// <summary>
/// Composition root for the Infrastructure layer. Everything registered here is an implementation
/// of an abstraction declared in the Application layer, which is what keeps the dependency
/// direction pointing inwards.
/// </summary>
public static class DependencyInjection
{
    public const string DefaultConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DefaultConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{DefaultConnectionStringName}' was not found. "
                + "Configure it in appsettings.json, user secrets or an environment variable.");

        // Singleton is safe here: the provider holds no scoped state, only the clock abstraction.
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<AuditableEntityInterceptor>();

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            // No retrying execution strategy is configured on purpose: it is incompatible with
            // user-initiated transactions, and IUnitOfWork.BeginTransactionAsync exposes those.
            options.UseSqlServer(
                connectionString,
                sqlServerOptions => sqlServerOptions
                    .MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

            options.AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IBusinessPartnerRepository, BusinessPartnerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();

        services.AddScoped<ITransactionNumberGenerator, TransactionNumberGenerator>();
        services.AddScoped<ITransactionReportService, TransactionReportService>();

        services.AddScoped<ApplicationDbContextInitialiser>();

        return services;
    }
}
