namespace Commerce.Store.Contracts.Currencies;

public sealed record CurrencyDto(
    int Id,
    string Code,
    string Name,
    string Symbol,
    string DisplayName,
    int DecimalPlaces,
    decimal Rate,
    bool IsActive,
    int DisplayOrder,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
