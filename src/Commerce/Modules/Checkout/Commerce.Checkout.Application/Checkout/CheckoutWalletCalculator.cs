using Commerce.Checkout.Contracts.Checkout;
using Commerce.Customers.Contracts.CustomerAccount;
using Commerce.Payments.Contracts.GiftCards;

namespace Commerce.Checkout.Application.Checkout;

public sealed class CheckoutWalletCalculator(
    IGiftCardValidationService giftCardValidationService,
    IStoreCreditReader storeCreditReader) : ICheckoutWalletCalculator
{
    public async Task<CheckoutWalletResult> CalculateAsync(
        CheckoutWalletContext context,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var giftCardApplied = 0m;
        var storeCreditApplied = 0m;
        var remaining = context.PayableTotal;

        if (!string.IsNullOrWhiteSpace(context.AppliedGiftCardCode) && remaining > 0m)
        {
            var validation = await giftCardValidationService.ValidateAsync(
                new GiftCardValidationRequest(
                    context.AppliedGiftCardCode,
                    context.StoreId,
                    context.CurrencyCode,
                    remaining,
                    DateTime.UtcNow),
                cancellationToken).ConfigureAwait(false);

            if (!validation.IsValid)
            {
                errors.AddRange(validation.Errors);
            }
            else
            {
                giftCardApplied = Math.Min(validation.AvailableBalance, remaining);
                remaining -= giftCardApplied;
            }
        }

        if (context.AppliedStoreCreditAmount > 0m && remaining > 0m)
        {
            if (!context.CustomerId.HasValue)
            {
                errors.Add("Store credit requires an authenticated customer.");
            }
            else
            {
                var creditResult = await storeCreditReader
                    .GetAvailableCreditAsync(
                        context.CustomerId.Value,
                        context.StoreId,
                        context.CurrencyCode,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (!creditResult.IsSuccess || creditResult.Value is null)
                {
                    errors.Add("Store credit account not available.");
                }
                else
                {
                    var available = creditResult.Value.Balance;
                    var requested = context.AppliedStoreCreditAmount;
                    if (requested > available)
                    {
                        errors.Add("Store credit amount exceeds available balance.");
                    }
                    else
                    {
                        storeCreditApplied = Math.Min(requested, remaining);
                        remaining -= storeCreditApplied;
                    }
                }
            }
        }

        var walletAdjustment = giftCardApplied + storeCreditApplied;
        var adjustedGrandTotal = Math.Max(0m, context.PayableTotal - walletAdjustment);

        return new CheckoutWalletResult(
            giftCardApplied,
            storeCreditApplied,
            walletAdjustment,
            adjustedGrandTotal,
            errors);
    }
}

public sealed record CheckoutWalletContext(
    int StoreId,
    int? CustomerId,
    string CurrencyCode,
    decimal PayableTotal,
    string? AppliedGiftCardCode,
    decimal AppliedStoreCreditAmount);

public sealed record CheckoutWalletResult(
    decimal GiftCardApplied,
    decimal StoreCreditApplied,
    decimal WalletAdjustmentTotal,
    decimal AdjustedGrandTotal,
    IReadOnlyList<string> Errors);

public interface ICheckoutWalletCalculator
{
    Task<CheckoutWalletResult> CalculateAsync(
        CheckoutWalletContext context,
        CancellationToken cancellationToken = default);
}
