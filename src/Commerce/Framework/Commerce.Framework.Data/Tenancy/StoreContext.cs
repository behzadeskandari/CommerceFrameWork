using Commerce.Framework.Contracts.Tenancy;

namespace Commerce.Framework.Data.Tenancy;

public sealed class StoreContext : IStoreContext
{
    public int? CurrentStoreId { get; private set; }

    public string? CurrentStoreSystemName { get; private set; }

    public string? CurrentStoreName { get; private set; }

    public int? CurrentLanguageId { get; private set; }

    public string? CurrentLanguageCode { get; private set; }

    public string? CurrentCultureCode { get; private set; }

    public bool IsRtl { get; private set; }

    public int? CurrentCurrencyId { get; private set; }

    public string? CurrentCurrencyCode { get; private set; }

    public bool HasStore => CurrentStoreId.HasValue;

    internal void SetStore(int storeId, string systemName, string storeName)
    {
        CurrentStoreId = storeId;
        CurrentStoreSystemName = systemName;
        CurrentStoreName = storeName;
    }

    internal void SetLanguage(int languageId, string languageCode, string cultureCode, bool isRtl)
    {
        CurrentLanguageId = languageId;
        CurrentLanguageCode = languageCode;
        CurrentCultureCode = cultureCode;
        IsRtl = isRtl;
    }

    internal void SetCurrency(int currencyId, string currencyCode)
    {
        CurrentCurrencyId = currencyId;
        CurrentCurrencyCode = currencyCode;
    }
}
