using Commerce.Framework.Core.Entities;

namespace Commerce.Tax.Domain.Entities;

public sealed class TaxZone : AggregateRoot
{
    public const int NameMaxLength = 200;
    public const int SystemNameMaxLength = 128;

    private readonly List<TaxZoneCountry> _countries = [];
    private readonly List<TaxZoneState> _states = [];
    private readonly List<TaxZonePostalRule> _postalRules = [];

    private TaxZone()
    {
    }

    public int StoreId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string SystemName { get; private set; } = string.Empty;

    public bool IsDefault { get; private set; }

    public bool IsActive { get; private set; }

    public int DisplayOrder { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<TaxZoneCountry> Countries => _countries;

    public IReadOnlyCollection<TaxZoneState> States => _states;

    public IReadOnlyCollection<TaxZonePostalRule> PostalRules => _postalRules;

    public static TaxZone Create(
        int storeId,
        string name,
        string systemName,
        bool isDefault,
        bool isActive,
        int displayOrder)
    {
        ValidateStore(storeId);
        ValidateName(name);
        ValidateSystemName(systemName);

        var utcNow = DateTime.UtcNow;
        return new TaxZone
        {
            StoreId = storeId,
            Name = name.Trim(),
            SystemName = systemName.Trim().ToLowerInvariant(),
            IsDefault = isDefault,
            IsActive = isActive,
            DisplayOrder = displayOrder,
            IsDeleted = false,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public void Update(string name, bool isDefault, bool isActive, int displayOrder)
    {
        EnsureNotDeleted();
        ValidateName(name);
        Name = name.Trim();
        IsDefault = isDefault;
        IsActive = isActive;
        DisplayOrder = displayOrder;
        Touch();
    }

    public void ReplaceCountries(IEnumerable<TaxZoneCountry> countries)
    {
        EnsureNotDeleted();
        _countries.Clear();
        _countries.AddRange(countries);
        Touch();
    }

    public void ReplaceStates(IEnumerable<TaxZoneState> states)
    {
        EnsureNotDeleted();
        _states.Clear();
        _states.AddRange(states);
        Touch();
    }

    public void ReplacePostalRules(IEnumerable<TaxZonePostalRule> rules)
    {
        EnsureNotDeleted();
        _postalRules.Clear();
        _postalRules.AddRange(rules);
        Touch();
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        IsActive = false;
        Touch();
    }

    public void LoadRules(
        IEnumerable<TaxZoneCountry> countries,
        IEnumerable<TaxZoneState> states,
        IEnumerable<TaxZonePostalRule> postalRules)
    {
        _countries.Clear();
        _countries.AddRange(countries);
        _states.Clear();
        _states.AddRange(states);
        _postalRules.Clear();
        _postalRules.AddRange(postalRules);
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("Tax zone has been deleted.");
        }
    }

    private static void ValidateStore(int storeId)
    {
        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }
    }

    private static void ValidateSystemName(string systemName)
    {
        if (string.IsNullOrWhiteSpace(systemName))
        {
            throw new ArgumentException("System name is required.", nameof(systemName));
        }
    }
}
