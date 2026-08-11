using Commerce.Framework.Core.Entities;

namespace Commerce.Store.Domain.Entities;

public sealed class Store : AggregateRoot
{
    public const int SystemNameMaxLength = 100;
    public const int NameMaxLength = 400;
    public const int UrlMaxLength = 1000;

    private readonly List<StoreDomain> _domains = [];

    private Store()
    {
    }

    public string SystemName { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Url { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public bool IsDeleted { get; private set; }

    public int DisplayOrder { get; private set; }

    public int DefaultLanguageId { get; private set; }

    public int DefaultCurrencyId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<StoreDomain> Domains => _domains.AsReadOnly();

    public static Store Create(
        string systemName,
        string name,
        string url,
        int defaultLanguageId,
        int defaultCurrencyId,
        int displayOrder = 0,
        bool isActive = true)
    {
        ValidateSystemName(systemName);
        ValidateName(name);

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Store URL is required.", nameof(url));
        }

        if (defaultLanguageId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(defaultLanguageId));
        }

        if (defaultCurrencyId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(defaultCurrencyId));
        }

        var now = DateTime.UtcNow;
        return new Store
        {
            SystemName = systemName.Trim(),
            Name = name.Trim(),
            Url = url.Trim(),
            DefaultLanguageId = defaultLanguageId,
            DefaultCurrencyId = defaultCurrencyId,
            DisplayOrder = displayOrder,
            IsActive = isActive,
            IsDeleted = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public StoreDomain AddDomain(string host, string scheme, int? port, bool isPrimary, bool isSslRequired)
    {
        EnsureNotDeleted();
        var domain = StoreDomain.Create(Id, host, scheme, port, isPrimary, isSslRequired);
        if (isPrimary)
        {
            foreach (var existing in _domains)
            {
                existing.ClearPrimary();
            }
        }

        _domains.Add(domain);
        UpdatedAtUtc = DateTime.UtcNow;
        return domain;
    }

    public void UpdateDetails(
        string name,
        string url,
        int defaultLanguageId,
        int defaultCurrencyId,
        int displayOrder,
        bool isActive)
    {
        EnsureNotDeleted();
        ValidateName(name);

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Store URL is required.", nameof(url));
        }

        Name = name.Trim();
        Url = url.Trim();
        DefaultLanguageId = defaultLanguageId;
        DefaultCurrencyId = defaultCurrencyId;
        DisplayOrder = displayOrder;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        EnsureNotDeleted();
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkDeleted()
    {
        IsDeleted = true;
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    internal void AttachDomain(StoreDomain domain)
    {
        _domains.Add(domain);
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("Deleted stores cannot be modified.");
        }
    }

    private static void ValidateSystemName(string systemName)
    {
        if (string.IsNullOrWhiteSpace(systemName))
        {
            throw new ArgumentException("System name is required.", nameof(systemName));
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }
    }
}
