using Commerce.Catalog.Application.Abstractions;
using Commerce.Catalog.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Catalog.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IProductRepository, EfProductRepository>();
        services.AddScoped<ICategoryRepository, EfCategoryRepository>();
        services.AddScoped<IProductCategoryRepository, EfProductCategoryRepository>();
        services.AddScoped<IProductAttributeRepository, EfProductAttributeRepository>();
        services.AddScoped<IProductVariantRepository, EfProductVariantRepository>();
        services.AddScoped<IProductOfferRepository, EfProductOfferRepository>();
        services.AddScoped<IProductMediaRepository, EfProductMediaRepository>();
        services.AddScoped<IProductVariantMediaRepository, EfProductVariantMediaRepository>();
        services.AddScoped<ICategoryMediaRepository, EfCategoryMediaRepository>();

        return services;
    }
}
