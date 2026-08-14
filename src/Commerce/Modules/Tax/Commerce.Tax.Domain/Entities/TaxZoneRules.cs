using Commerce.Framework.Core.Entities;
using Commerce.Tax.Domain.Enums;

namespace Commerce.Tax.Domain.Entities;

public sealed class TaxZoneCountry : Entity
{
    private TaxZoneCountry()
    {
    }

    public int TaxZoneId { get; private set; }

    public string CountryCode { get; private set; } = string.Empty;

    public static TaxZoneCountry Create(int zoneId, string countryCode)
    {
        if (zoneId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(zoneId));
        }

        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Trim().Length != 2)
        {
            throw new ArgumentException("Country code must be ISO 3166-1 alpha-2.", nameof(countryCode));
        }

        return new TaxZoneCountry
        {
            TaxZoneId = zoneId,
            CountryCode = countryCode.Trim().ToUpperInvariant()
        };
    }
}

public sealed class TaxZoneState : Entity
{
    private TaxZoneState()
    {
    }

    public int TaxZoneId { get; private set; }

    public string CountryCode { get; private set; } = string.Empty;

    public string StateProvince { get; private set; } = string.Empty;

    public static TaxZoneState Create(int zoneId, string countryCode, string stateProvince)
    {
        if (zoneId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(zoneId));
        }

        if (string.IsNullOrWhiteSpace(countryCode))
        {
            throw new ArgumentException("Country code is required.", nameof(countryCode));
        }

        if (string.IsNullOrWhiteSpace(stateProvince))
        {
            throw new ArgumentException("State/province is required.", nameof(stateProvince));
        }

        return new TaxZoneState
        {
            TaxZoneId = zoneId,
            CountryCode = countryCode.Trim().ToUpperInvariant(),
            StateProvince = stateProvince.Trim()
        };
    }
}

public sealed class TaxZonePostalRule : Entity
{
    private TaxZonePostalRule()
    {
    }

    public int TaxZoneId { get; private set; }

    public string CountryCode { get; private set; } = string.Empty;

    public PostalRuleType RuleType { get; private set; }

    public string PostalFrom { get; private set; } = string.Empty;

    public string? PostalTo { get; private set; }

    public static TaxZonePostalRule CreateExact(int zoneId, string countryCode, string postalCode) =>
        Create(zoneId, countryCode, PostalRuleType.Exact, postalCode, null);

    public static TaxZonePostalRule CreatePrefix(int zoneId, string countryCode, string prefix) =>
        Create(zoneId, countryCode, PostalRuleType.Prefix, prefix, null);

    public static TaxZonePostalRule CreateRange(int zoneId, string countryCode, string from, string to) =>
        Create(zoneId, countryCode, PostalRuleType.Range, from, to);

    private static TaxZonePostalRule Create(
        int zoneId,
        string countryCode,
        PostalRuleType ruleType,
        string postalFrom,
        string? postalTo)
    {
        if (zoneId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(zoneId));
        }

        if (string.IsNullOrWhiteSpace(countryCode))
        {
            throw new ArgumentException("Country code is required.", nameof(countryCode));
        }

        if (string.IsNullOrWhiteSpace(postalFrom))
        {
            throw new ArgumentException("Postal code is required.", nameof(postalFrom));
        }

        return new TaxZonePostalRule
        {
            TaxZoneId = zoneId,
            CountryCode = countryCode.Trim().ToUpperInvariant(),
            RuleType = ruleType,
            PostalFrom = postalFrom.Trim().ToUpperInvariant(),
            PostalTo = string.IsNullOrWhiteSpace(postalTo) ? null : postalTo.Trim().ToUpperInvariant()
        };
    }

    public bool Matches(string postalCode)
    {
        var normalized = postalCode.Trim().ToUpperInvariant();
        return RuleType switch
        {
            PostalRuleType.Exact => normalized == PostalFrom,
            PostalRuleType.Prefix => normalized.StartsWith(PostalFrom, StringComparison.Ordinal),
            PostalRuleType.Range => PostalTo is not null &&
                string.Compare(normalized, PostalFrom, StringComparison.Ordinal) >= 0 &&
                string.Compare(normalized, PostalTo, StringComparison.Ordinal) <= 0,
            _ => false
        };
    }
}
