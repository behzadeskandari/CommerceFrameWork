namespace Commerce.Catalog.Contracts.Products;

public interface ICatalogChangeNotifier
{
    Task NotifyProductCreatedAsync(int productId, CancellationToken cancellationToken = default);

    Task NotifyProductUpdatedAsync(int productId, CancellationToken cancellationToken = default);

    Task NotifyProductDeletedAsync(int productId, CancellationToken cancellationToken = default);
}
