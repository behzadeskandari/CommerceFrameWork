namespace Commerce.Store.Contracts.Languages;

public sealed record LanguageDto(
    int Id,
    string Name,
    string LanguageCode,
    string CultureCode,
    string NativeName,
    bool IsActive,
    bool IsRtl,
    int DisplayOrder,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
