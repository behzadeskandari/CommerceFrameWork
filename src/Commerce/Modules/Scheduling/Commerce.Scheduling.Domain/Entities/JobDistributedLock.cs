namespace Commerce.Scheduling.Domain.Entities;

public sealed class JobDistributedLock
{
    public const int LockKeyMaxLength = 256;
    public const int OwnerIdMaxLength = 128;

    public string LockKey { get; private set; } = string.Empty;

    public string OwnerId { get; private set; } = string.Empty;

    public DateTime AcquiredAtUtc { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public static JobDistributedLock Create(string lockKey, string ownerId, DateTime acquiredAtUtc, DateTime expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(lockKey) || lockKey.Length > LockKeyMaxLength)
        {
            throw new ArgumentException("Lock key is required.", nameof(lockKey));
        }

        if (string.IsNullOrWhiteSpace(ownerId) || ownerId.Length > OwnerIdMaxLength)
        {
            throw new ArgumentException("Owner id is required.", nameof(ownerId));
        }

        return new JobDistributedLock
        {
            LockKey = lockKey.Trim(),
            OwnerId = ownerId.Trim(),
            AcquiredAtUtc = acquiredAtUtc,
            ExpiresAtUtc = expiresAtUtc
        };
    }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAtUtc;
}
