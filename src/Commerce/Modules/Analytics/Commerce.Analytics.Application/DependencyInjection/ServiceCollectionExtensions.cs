using Commerce.Analytics.Application.Dashboard;
using Commerce.Analytics.Application.Reports;
using Commerce.Analytics.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Analytics.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAnalyticsApplication(this IServiceCollection services)
    {
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IReportsService, ReportsService>();
        return services;
    }
}
