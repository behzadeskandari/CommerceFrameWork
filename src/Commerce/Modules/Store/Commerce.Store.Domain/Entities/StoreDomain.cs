using Commerce.Framework.Core.Entities;

namespace Commerce.Store.Domain.Entities;

public sealed class StoreDomain : Entity
{
    public const int HostMaxLength = 255;
    public const int SchemeMaxLength = 10;

    private StoreDomain()
    {
    }

    public int StoreId { get; private set; }

    public string Host { get; private set; } = string.Empty;

    public int? Port { get; private set; }

    public string Scheme { get; private set; } = "https";

    public bool IsPrimary { get; private set; }

    public bool IsSslRequired { get; private set; }

    public static StoreDomain Create(
        int storeId,
        string host,
        string scheme,
        int? port,
        bool isPrimary,
        bool isSslRequired)
    {
        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }

        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("Host is required.", nameof(host));
        }

        return new StoreDomain
        {
            StoreId = storeId,
            Host = host.Trim().ToLowerInvariant(),
            Scheme = string.IsNullOrWhiteSpace(scheme) ? "https" : scheme.Trim().ToLowerInvariant(),
            Port = port,
            IsPrimary = isPrimary,
            IsSslRequired = isSslRequired
        };
    }

    public void Update(string host, string scheme, int? port, bool isPrimary, bool isSslRequired)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("Host is required.", nameof(host));
        }

        Host = host.Trim().ToLowerInvariant();
        Scheme = string.IsNullOrWhiteSpace(scheme) ? "https" : scheme.Trim().ToLowerInvariant();
        Port = port;
        IsPrimary = isPrimary;
        IsSslRequired = isSslRequired;
    }

    internal void ClearPrimary() => IsPrimary = false;
}
