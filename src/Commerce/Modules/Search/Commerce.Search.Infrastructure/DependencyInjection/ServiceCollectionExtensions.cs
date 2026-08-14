using Commerce.Catalog.Contracts.Products;
using Commerce.Framework.Scheduling;
using Commerce.Search.Application;
using Commerce.Search.Application.DependencyInjection;
using Commerce.Search.Application.Abstractions;
using Commerce.Search.Application.Jobs;
using Commerce.Search.Contracts;
using Commerce.Search.Infrastructure.Persistence.Repositories;
using Commerce.Search.Infrastructure.Security;
using Commerce.Framework.Contracts.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Search.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSearchInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IModulePermissionContributor, SearchPermissionContributor>();
        services.AddScoped<ISearchRepository, EfSearchRepository>();
        services.AddScoped<SearchIndexCoordinator>();
        services.AddScoped<ISearchIndexCoordinator>(sp => sp.GetRequiredService<SearchIndexCoordinator>());
        services.AddScoped<ICatalogChangeNotifier>(sp => sp.GetRequiredService<SearchIndexCoordinator>());
        services.AddScoped<IBackgroundJobHandler, SearchIndexProcessJobHandler>();
        services.AddSearchApplication();
        return services;
    }
}
