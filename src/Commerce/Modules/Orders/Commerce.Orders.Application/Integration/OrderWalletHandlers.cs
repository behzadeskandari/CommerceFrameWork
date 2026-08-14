using Commerce.Customers.Contracts.Affiliates;
using Commerce.Customers.Contracts.CustomerAccount;
using Commerce.Orders.Application.Abstractions;
using Commerce.Orders.Contracts.Orders;
using Commerce.Payments.Contracts.GiftCards;

namespace Commerce.Orders.Application.Integration;

public sealed class OrderWalletConsumptionHandler(
    IOrderRepository orderRepository,
    IStoreCreditService storeCreditService,
    IGiftCardRedemptionService giftCardRedemptionService) : IOrderCreatedHandler
{
    public async Task HandleOrderCreatedAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return;
        }

        if (order.StoreCreditApplied > 0m && order.CustomerId.HasValue)
        {
            await storeCreditService.DebitAsync(
                order.CustomerId.Value,
                order.StoreId,
                order.CurrencyCode,
                new ApplyStoreCreditRequest(order.StoreCreditApplied, orderId, "Applied at checkout."),
                $"order-{orderId}-store-credit",
                cancellationToken).ConfigureAwait(false);
        }

        if (order.GiftCardApplied > 0m && !string.IsNullOrWhiteSpace(order.AppliedGiftCardCode))
        {
            await giftCardRedemptionService.TryRedeemAsync(
                new GiftCardRedemptionRequest(
                    order.AppliedGiftCardCode,
                    order.StoreId,
                    order.CurrencyCode,
                    order.GiftCardApplied,
                    orderId,
                    $"order-{orderId}-giftcard"),
                cancellationToken).ConfigureAwait(false);
        }
    }
}

public sealed class OrderPaidAffiliateCommissionHandler(
    IOrderRepository orderRepository,
    IAffiliateReader affiliateReader,
    IAffiliateCommissionService commissionService,
    IAffiliateReferralService referralService) : IOrderPaidHandler
{
    public async Task HandleOrderPaidAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order is null || !order.AffiliateId.HasValue)
        {
            return;
        }

        var affiliateResult = await affiliateReader.GetAsync(order.AffiliateId.Value, cancellationToken).ConfigureAwait(false);
        if (!affiliateResult.IsSuccess || affiliateResult.Value is null || !affiliateResult.Value.IsActive)
        {
            return;
        }

        var affiliate = affiliateResult.Value;
        var commissionBase = order.Subtotal - order.DiscountTotal + order.ShippingTotal;
        if (commissionBase <= 0m)
        {
            return;
        }

        if (order.CustomerId.HasValue)
        {
            await referralService.RecordReferralAsync(
                affiliate.Id,
                order.CustomerId.Value,
                order.StoreId,
                cancellationToken).ConfigureAwait(false);
        }

        await commissionService.EarnCommissionAsync(
            affiliate.Id,
            order.StoreId,
            order.CurrencyCode,
            commissionBase,
            affiliate.CommissionRatePercent,
            orderId,
            $"order-{orderId}-affiliate-commission",
            cancellationToken).ConfigureAwait(false);
    }
}
