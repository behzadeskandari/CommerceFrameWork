using Commerce.Tax.Domain.Entities;

namespace Commerce.Tax.Application.Tax;

/// <summary>
/// Deterministic zone matching:
/// 1. Postal code rule
/// 2. State/province within country
/// 3. Country
/// 4. Default zone
/// </summary>
public static class TaxZoneMatcher
{
    public static TaxZone? MatchZone(
        IReadOnlyList<TaxZone> zones,
        string countryCode,
        string? stateProvince,
        string postalCode)
    {
        var active = zones.Where(z => z.IsActive && !z.IsDeleted).ToList();
        if (active.Count == 0)
        {
            return null;
        }

        var country = countryCode.Trim().ToUpperInvariant();
        var state = string.IsNullOrWhiteSpace(stateProvince) ? null : stateProvince.Trim();
        var postal = postalCode.Trim().ToUpperInvariant();

        var postalMatch = active
            .Where(z => z.PostalRules.Any(r => r.CountryCode == country && r.Matches(postal)))
            .OrderByDescending(z => z.PostalRules.Count(r => r.CountryCode == country && r.Matches(postal)))
            .ThenBy(z => z.DisplayOrder)
            .FirstOrDefault();

        if (postalMatch is not null)
        {
            return postalMatch;
        }

        if (state is not null)
        {
            var stateMatch = active
                .Where(z => z.States.Any(s => s.CountryCode == country &&
                    string.Equals(s.StateProvince, state, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(z => z.DisplayOrder)
                .FirstOrDefault();

            if (stateMatch is not null)
            {
                return stateMatch;
            }
        }

        var countryMatch = active
            .Where(z => z.Countries.Any(c => c.CountryCode == country))
            .OrderBy(z => z.DisplayOrder)
            .FirstOrDefault();

        if (countryMatch is not null)
        {
            return countryMatch;
        }

        return active
            .Where(z => z.IsDefault)
            .OrderBy(z => z.DisplayOrder)
            .FirstOrDefault();
    }
}
