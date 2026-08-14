namespace Commerce.Downloads.Contracts.Downloads;

public interface IDownloadAvailableHandler
{
    Task HandleDownloadAvailableAsync(
        int customerId,
        int orderId,
        int productId,
        CancellationToken cancellationToken = default);
}
