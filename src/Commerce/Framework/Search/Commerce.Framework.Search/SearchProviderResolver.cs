using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Commerce.Framework.Search;

public sealed class SearchProviderResolver(
    IEnumerable<ISearchProvider> providers,
    IEnumerable<ISearchIndexer> indexers,
    ILogger<SearchProviderResolver> logger) : ISearchProviderResolver
{
    public ISearchProvider ResolveProvider() =>
        providers.FirstOrDefault(p => p.SystemName.Equals(DefaultSearchProviderNames.Database, StringComparison.OrdinalIgnoreCase))
        ?? providers.FirstOrDefault()
        ?? throw new InvalidOperationException("No ISearchProvider implementations are registered.");

    public ISearchIndexer ResolveIndexer()
    {
        var provider = ResolveProvider();
        var indexer = indexers.FirstOrDefault(i =>
            i.SystemName.Equals(provider.SystemName, StringComparison.OrdinalIgnoreCase));

        if (indexer is null)
        {
            logger.LogError("No ISearchIndexer registered for provider {Provider}.", provider.SystemName);
            throw new InvalidOperationException($"No indexer registered for provider '{provider.SystemName}'.");
        }

        return indexer;
    }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCommerceSearch(this IServiceCollection services)
    {
        services.AddScoped<ISearchProviderResolver, SearchProviderResolver>();
        return services;
    }
}
