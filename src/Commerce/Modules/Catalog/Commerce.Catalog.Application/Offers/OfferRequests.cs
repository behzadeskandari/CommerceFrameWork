namespace Commerce.Catalog.Application.Offers;

public sealed record CreateOfferRequest(
    int ProductId,
    int? VariantId,
    int StoreId,
    int CurrencyId,
    string CurrencyCode,
    decimal Price,
    decimal? CompareAtPrice = null,
    bool IsActive = true,
    DateTime? ValidFromUtc = null,
    DateTime? ValidToUtc = null);

public sealed record UpdateOfferRequest(
    decimal Price,
    decimal? CompareAtPrice = null,
    bool IsActive = true,
    DateTime? ValidFromUtc = null,
    DateTime? ValidToUtc = null);
