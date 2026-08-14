using Commerce.Catalog.Contracts.Products;
using Commerce.Pricing.Application.Abstractions;

namespace Commerce.Pricing.Application.Pricing;

public sealed class ProductCategoryLookup(IProductReader productReader) : IProductCategoryLookup
{
    public async Task<IReadOnlyDictionary<int, IReadOnlyList<int>>> GetCategoryIdsByProductIdsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<int, IReadOnlyList<int>>();

        foreach (var productId in productIds.Distinct())
        {
            var productResult = await productReader.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
            result[productId] = productResult.IsSuccess
                ? productResult.Value!.CategoryIds
                : [];
        }

        return result;
    }
}
