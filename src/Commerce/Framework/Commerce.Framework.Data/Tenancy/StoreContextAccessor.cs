using Commerce.Framework.Contracts.Tenancy;

namespace Commerce.Framework.Data.Tenancy;

public sealed class StoreContextAccessor : IStoreContextAccessor
{
    public StoreContextAccessor(StoreContext storeContext)
    {
        StoreContext = storeContext;
    }

    public IStoreContext StoreContext { get; }

    public void SetStore(int storeId, string systemName, string storeName)
    {
        if (StoreContext is StoreContext mutable)
        {
            mutable.SetStore(storeId, systemName, storeName);
        }
    }

    public void SetLanguage(int languageId, string languageCode, string cultureCode, bool isRtl)
    {
        if (StoreContext is StoreContext mutable)
        {
            mutable.SetLanguage(languageId, languageCode, cultureCode, isRtl);
        }
    }

    public void SetCurrency(int currencyId, string currencyCode)
    {
        if (StoreContext is StoreContext mutable)
        {
            mutable.SetCurrency(currencyId, currencyCode);
        }
    }
}
