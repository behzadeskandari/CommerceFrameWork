namespace Commerce.Store.Application.Stores;

public sealed record CreateStoreRequest(
    string SystemName,
    string Name,
    string Url,
    int DefaultLanguageId,
    int DefaultCurrencyId,
    int DisplayOrder = 0,
    bool IsActive = true,
    IReadOnlyList<CreateStoreDomainRequest>? Domains = null);

public sealed record CreateStoreDomainRequest(
    string Host,
    string Scheme,
    int? Port,
    bool IsPrimary,
    bool IsSslRequired);

public sealed record UpdateStoreRequest(
    string Name,
    string Url,
    int DefaultLanguageId,
    int DefaultCurrencyId,
    int DisplayOrder,
    bool IsActive);

public sealed record AddStoreDomainRequest(
    string Host,
    string Scheme,
    int? Port,
    bool IsPrimary,
    bool IsSslRequired);

public sealed record UpdateStoreDomainRequest(
    string Host,
    string Scheme,
    int? Port,
    bool IsPrimary,
    bool IsSslRequired);
