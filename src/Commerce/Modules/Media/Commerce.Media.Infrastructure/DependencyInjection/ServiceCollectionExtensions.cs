using Commerce.Framework.Contracts.Configuration;
using Commerce.Media.Application.Abstractions;
using Commerce.Media.Application.DependencyInjection;
using Commerce.Media.Contracts.Images;
using Commerce.Media.Contracts.Storage;
using Commerce.Media.Contracts.Urls;
using Commerce.Media.Infrastructure.Configuration;
using Commerce.Media.Infrastructure.Images;
using Commerce.Media.Infrastructure.Persistence.Repositories;
using Commerce.Media.Infrastructure.Storage;
using Commerce.Media.Infrastructure.Urls;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Media.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediaInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MediaStorageOptions>(configuration.GetSection(MediaStorageOptions.SectionName));
        services.AddSingleton<ISettingDefinitionProvider, MediaSettingDefinitionProvider>();
        services.AddScoped<IMediaAssetRepository, EfMediaAssetRepository>();
        services.AddSingleton<IMediaStorage, LocalMediaStorage>();
        services.AddSingleton<IImageProcessor, BasicImageProcessor>();
        services.AddSingleton<IMediaUrlResolver, MediaUrlResolver>();
        services.AddMediaApplication();
        return services;
    }
}
