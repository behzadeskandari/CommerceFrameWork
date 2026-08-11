using Commerce.Media.Application;
using Commerce.Media.Application.Abstractions;
using Commerce.Media.Contracts.Media;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Media.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediaApplication(this IServiceCollection services)
    {
        services.AddScoped<MediaSettings>();
        services.AddScoped<MediaUploadValidator>();
        services.AddScoped<MediaService>();
        services.AddScoped<IMediaService>(sp => sp.GetRequiredService<MediaService>());
        services.AddScoped<IMediaReader>(sp => sp.GetRequiredService<MediaService>());
        return services;
    }
}
