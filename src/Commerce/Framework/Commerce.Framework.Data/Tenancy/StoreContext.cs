using Commerce.Framework.Contracts.Tenancy;

namespace Commerce.Framework.Data.Tenancy;

public sealed class StoreContext : IStoreContext
{
    public int? CurrentStoreId { get; private set; }

    public string? CurrentStoreName { get; private set; }

    public bool HasStore => CurrentStoreId.HasValue;

    internal void SetStore(int storeId, string storeName)
    {
        CurrentStoreId = storeId;
        CurrentStoreName = storeName;
    }
}
