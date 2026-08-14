namespace Commerce.Promotions.Contracts.Usage;

public sealed record PromotionOrderUsageRequest(
    int StoreId,
    int OrderId,
    int? CustomerId,
    string? CouponCode,
    DateTime CurrentTimeUtc);

public interface IPromotionUsageService
{
    Task RecordOrderUsageAsync(
        PromotionOrderUsageRequest request,
        CancellationToken cancellationToken = default);
}
