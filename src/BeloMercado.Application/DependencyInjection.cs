using BeloMercado.Application.Interfaces.Audit;
using BeloMercado.Application.Services;
using BeloMercado.Application.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeloMercado.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<AuthSettings>(configuration.GetSection(AuthSettings.SectionName));

        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<AuthAccessService>();
        services.AddScoped<StoreManagementService>();
        services.AddScoped<UserManagementService>();
        services.AddScoped<CatalogService>();
        services.AddScoped<InventoryService>();
        services.AddScoped<SalesService>();
        services.AddScoped<ProcurementService>();
        services.AddScoped<ConsolidationService>();
        services.AddScoped<ReportingService>();

        return services;
    }
}
