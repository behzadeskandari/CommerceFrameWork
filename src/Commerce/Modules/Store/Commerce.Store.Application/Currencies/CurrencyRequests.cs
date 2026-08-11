namespace Commerce.Store.Application.Currencies;

public sealed record CreateCurrencyRequest(
    string Code,
    string Name,
    string? Symbol,
    string? DisplayName,
    decimal Rate,
    int DecimalPlaces = 2,
    int DisplayOrder = 0,
    bool IsActive = true);

public sealed record UpdateCurrencyRequest(
    string Name,
    string? Symbol,
    string? DisplayName,
    decimal Rate,
    int DecimalPlaces,
    int DisplayOrder,
    bool IsActive);
