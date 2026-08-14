using System.Collections.Concurrent;

namespace Commerce.Framework.Infrastructure.Caching;

public sealed class CacheKeyRegistry
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _tagToKeys = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _prefixToKeys = new(StringComparer.Ordinal);

    public void Track(string key, IReadOnlyList<string> tags, string prefix)
    {
        foreach (var tag in tags)
        {
            var keys = _tagToKeys.GetOrAdd(tag, _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal));
            keys.TryAdd(key, 0);
        }

        var prefixKeys = _prefixToKeys.GetOrAdd(prefix, _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal));
        prefixKeys.TryAdd(key, 0);
    }

    public void Untrack(string key, IReadOnlyList<string> tags, string prefix)
    {
        foreach (var tag in tags)
        {
            if (_tagToKeys.TryGetValue(tag, out var keys))
            {
                keys.TryRemove(key, out _);
            }
        }

        if (_prefixToKeys.TryGetValue(prefix, out var prefixKeys))
        {
            prefixKeys.TryRemove(key, out _);
        }
    }

    public IReadOnlyList<string> GetKeysForTag(string tag) =>
        _tagToKeys.TryGetValue(tag, out var keys)
            ? keys.Keys.ToList()
            : [];

    public IReadOnlyList<string> GetKeysForPrefix(string prefix) =>
        _prefixToKeys.TryGetValue(prefix, out var keys)
            ? keys.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList()
            : [];
}
