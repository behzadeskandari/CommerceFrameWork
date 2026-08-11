namespace Commerce.Framework.Contracts.Tenancy;

public interface IStoreContext
{
    int? CurrentStoreId { get; }
    string? CurrentStoreName { get; }
    bool HasStore { get; }
}
