using Commerce.Observability.Application.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Observability.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddObservabilityApplication(this IServiceCollection services) => services;
}
