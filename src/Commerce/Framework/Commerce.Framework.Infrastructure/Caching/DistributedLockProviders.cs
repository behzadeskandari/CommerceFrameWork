using Commerce.Framework.Contracts.Caching;
using System.Collections.Concurrent;

namespace Commerce.Framework.Infrastructure.Caching;

public sealed class InMemoryDistributedLockProvider : IDistributedLockProvider
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    public async Task<IDistributedLockHandle> AcquireAsync(
        string resource,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var semaphore = _locks.GetOrAdd(resource, _ => new SemaphoreSlim(1, 1));
        var acquired = await semaphore.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
        return new Handle(semaphore, acquired);
    }

    private sealed class Handle(SemaphoreSlim semaphore, bool acquired) : IDistributedLockHandle
    {
        private int _disposed;

        public bool IsAcquired { get; } = acquired;

        public ValueTask DisposeAsync()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
            {
                return ValueTask.CompletedTask;
            }

            if (IsAcquired)
            {
                semaphore.Release();
            }

            return ValueTask.CompletedTask;
        }
    }
}

public sealed class NoOpDistributedLockHandle : IDistributedLockHandle
{
    public static NoOpDistributedLockHandle Instance { get; } = new();

    public bool IsAcquired => true;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public sealed class NoOpDistributedLockProvider : IDistributedLockProvider
{
    public Task<IDistributedLockHandle> AcquireAsync(
        string resource,
        TimeSpan timeout,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IDistributedLockHandle>(NoOpDistributedLockHandle.Instance);
}
