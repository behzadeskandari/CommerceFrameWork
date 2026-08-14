using Commerce.Framework.Contracts.Caching;

namespace Commerce.Framework.Infrastructure.Caching;

public static class CacheGuard
{
    public static void EnsureSafeKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        foreach (var segment in CacheDeniedSegments.Segments)
        {
            if (key.Contains(segment, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Cache key '{key}' is denied because it may contain mutable financial or transactional state.");
            }
        }
    }
}
