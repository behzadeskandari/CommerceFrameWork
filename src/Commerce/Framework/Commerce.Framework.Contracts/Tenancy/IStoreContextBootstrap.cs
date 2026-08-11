namespace Commerce.Framework.Contracts.Tenancy;

public interface IStoreContextBootstrap
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
