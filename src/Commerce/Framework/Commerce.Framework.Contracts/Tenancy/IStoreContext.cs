namespace Commerce.Framework.Contracts.Tenancy;

public interface IStoreContext
{
    int? CurrentStoreId { get; }

    string? CurrentStoreSystemName { get; }

    string? CurrentStoreName { get; }

    bool HasStore { get; }

    int? CurrentLanguageId { get; }

    string? CurrentLanguageCode { get; }

    string? CurrentCultureCode { get; }

    bool IsRtl { get; }

    int? CurrentCurrencyId { get; }

    string? CurrentCurrencyCode { get; }
}
