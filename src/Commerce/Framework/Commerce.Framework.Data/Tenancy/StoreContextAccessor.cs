using Commerce.Framework.Contracts.Tenancy;

namespace Commerce.Framework.Data.Tenancy;

public sealed class StoreContextAccessor : IStoreContextAccessor
{
    public StoreContextAccessor(StoreContext storeContext)
    {
        StoreContext = storeContext;
    }

    public IStoreContext StoreContext { get; }

    public void SetStore(int storeId, string storeName)
    {
        if (StoreContext is StoreContext mutable)
        {
            mutable.SetStore(storeId, storeName);
        }
    }
}
