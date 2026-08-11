namespace Commerce.Framework.Contracts.Tenancy;

public interface IStoreContextAccessor
{
    IStoreContext StoreContext { get; }

    void SetStore(int storeId, string storeName);
}
