namespace Commerce.Store.Application.Languages;

public sealed record CreateLanguageRequest(
    string Name,
    string LanguageCode,
    string CultureCode,
    string? NativeName,
    bool IsRtl,
    int DisplayOrder = 0,
    bool IsActive = true);

public sealed record UpdateLanguageRequest(
    string Name,
    string CultureCode,
    string? NativeName,
    bool IsRtl,
    int DisplayOrder,
    bool IsActive);
