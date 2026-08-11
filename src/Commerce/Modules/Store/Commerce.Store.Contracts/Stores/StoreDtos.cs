namespace Commerce.Store.Contracts.Stores;

public sealed record StoreSummaryDto(
    int Id,
    string SystemName,
    string Name,
    string Url,
    bool IsActive,
    int DisplayOrder,
    int DefaultLanguageId,
    int DefaultCurrencyId,
    DateTime CreatedAtUtc);

public sealed record StoreDomainDto(
    int Id,
    int StoreId,
    string Host,
    string Scheme,
    int? Port,
    bool IsPrimary,
    bool IsSslRequired);

public sealed record StoreDetailDto(
    int Id,
    string SystemName,
    string Name,
    string Url,
    bool IsActive,
    int DisplayOrder,
    int DefaultLanguageId,
    int DefaultCurrencyId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<StoreDomainDto> Domains);

public sealed record StoreContextDto(
    int? StoreId,
    string? StoreSystemName,
    string? StoreName,
    int? LanguageId,
    string? LanguageCode,
    string? CultureCode,
    bool IsRtl,
    int? CurrencyId,
    string? CurrencyCode);
