using Commerce.Downloads.Application.Abstractions;
using Commerce.Downloads.Application.DependencyInjection;
using Commerce.Downloads.Contracts.Storage;
using Commerce.Downloads.Infrastructure.Media;
using Commerce.Downloads.Infrastructure.Persistence.Repositories;
using Commerce.Downloads.Infrastructure.Security;
using Commerce.Downloads.Infrastructure.Storage;
using Commerce.Framework.Contracts.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Downloads.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDownloadsInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IModulePermissionContributor, DownloadPermissionContributor>();
        services.AddScoped<IDownloadRepository, EfDownloadRepository>();
        services.AddScoped<IDownloadMediaResolver, DownloadMediaResolver>();
        services.AddScoped<IDownloadStorage, MediaDownloadStorage>();
        services.AddDownloadsApplication();
        return services;
    }
}
