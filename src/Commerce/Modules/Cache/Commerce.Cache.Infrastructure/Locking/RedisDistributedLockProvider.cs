using Commerce.Framework.Contracts.Caching;
using StackExchange.Redis;

namespace Commerce.Cache.Infrastructure.Locking;

public sealed class RedisDistributedLockProvider(IConnectionMultiplexer connectionMultiplexer) : IDistributedLockProvider
{
    private const string LockPrefix = "commerce:lock:";

    public async Task<IDistributedLockHandle> AcquireAsync(
        string resource,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var db = connectionMultiplexer.GetDatabase();
        var lockKey = LockPrefix + resource;
        var lockValue = Guid.NewGuid().ToString("N");
        var expiry = timeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(10) : timeout;
        var started = Environment.TickCount64;

        while (Environment.TickCount64 - started < expiry.TotalMilliseconds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await db.StringSetAsync(lockKey, lockValue, expiry, When.NotExists).ConfigureAwait(false))
            {
                return new Handle(db, lockKey, lockValue);
            }

            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }

        return new Handle(db, lockKey, lockValue, acquired: false);
    }

    private sealed class Handle(IDatabase database, string lockKey, string lockValue, bool acquired = true) : IDistributedLockHandle
    {
        private int _disposed;

        public bool IsAcquired { get; } = acquired;

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0 || !IsAcquired)
            {
                return;
            }

            const string releaseScript = """
                if redis.call('get', KEYS[1]) == ARGV[1] then
                    return redis.call('del', KEYS[1])
                else
                    return 0
                end
                """;

            await database.ScriptEvaluateAsync(releaseScript, [lockKey], [lockValue]).ConfigureAwait(false);
        }
    }
}
