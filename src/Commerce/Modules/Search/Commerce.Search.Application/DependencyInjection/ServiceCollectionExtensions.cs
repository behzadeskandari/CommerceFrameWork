using Commerce.Search.Application;
using Commerce.Search.Application.Indexing;
using Commerce.Search.Application.Storefront;
using Commerce.Search.Contracts;
using Commerce.Search.Contracts.Admin;
using Commerce.Search.Contracts.Storefront;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Search.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSearchApplication(this IServiceCollection services)
    {
        services.AddScoped<ISearchQueryService, SearchQueryService>();
        services.AddScoped<ISearchStorefrontService, SearchStorefrontService>();
        services.AddScoped<ISearchAdminService, SearchAdminService>();
        services.AddScoped<SearchDocumentBuilder>();
        return services;
    }
}
