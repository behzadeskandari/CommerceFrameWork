using Commerce.Catalog.Contracts.Products;
using Commerce.Checkout.Contracts.Checkout;

namespace Commerce.Checkout.Application.Checkout;

public sealed class CheckoutRequiresShippingEvaluator(IProductReader productReader)
{
    public async Task<bool> RequiresShippingAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default)
    {
        foreach (var productId in productIds.Distinct())
        {
            var product = await productReader.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
            if (!product.IsSuccess || product.Value is null)
            {
                continue;
            }

            if (!IsDigitalOnly(product.Value.ProductType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDigitalOnly(string productType) =>
        string.Equals(productType, "Digital", StringComparison.OrdinalIgnoreCase);
}
