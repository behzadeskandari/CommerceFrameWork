using Commerce.Promotions.Application.Abstractions;
using Commerce.Promotions.Contracts.Usage;

namespace Commerce.Promotions.Application.Usage;

public sealed class PromotionUsageService(IPromotionsRepository repository) : IPromotionUsageService
{
    public async Task RecordOrderUsageAsync(
        PromotionOrderUsageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.CouponCode))
        {
            return;
        }

        var normalized = request.CouponCode.Trim().ToUpperInvariant();
        var promotions = await repository
            .GetActivePromotionsAsync(request.StoreId, request.CurrentTimeUtc, cancellationToken)
            .ConfigureAwait(false);

        foreach (var promotion in promotions.Where(x =>
                     x.RequiresCouponCode &&
                     string.Equals(x.CouponCode, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            if (!promotion.HasGlobalUsageRemaining())
            {
                continue;
            }

            if (request.CustomerId.HasValue && promotion.PerCustomerUsageLimit.HasValue)
            {
                var customerUsage = await repository
                    .GetCustomerUsageCountAsync(promotion.Id, request.CustomerId.Value, cancellationToken)
                    .ConfigureAwait(false);

                if (customerUsage >= promotion.PerCustomerUsageLimit.Value)
                {
                    continue;
                }
            }

            var usage = Domain.Entities.PromotionUsage.Record(
                promotion.Id,
                request.CustomerId,
                request.OrderId,
                request.CurrentTimeUtc);

            promotion.RecordUsage();
            await repository.AddUsageAsync(usage, cancellationToken).ConfigureAwait(false);
            await repository.SavePromotionAsync(promotion, cancellationToken).ConfigureAwait(false);
        }
    }
}
