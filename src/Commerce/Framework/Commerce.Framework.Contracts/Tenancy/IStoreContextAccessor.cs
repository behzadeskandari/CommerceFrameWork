namespace Commerce.Framework.Contracts.Tenancy;

public interface IStoreContextAccessor
{
    IStoreContext StoreContext { get; }

    void SetStore(int storeId, string systemName, string storeName);

    void SetLanguage(int languageId, string languageCode, string cultureCode, bool isRtl);

    void SetCurrency(int currencyId, string currencyCode);
}
