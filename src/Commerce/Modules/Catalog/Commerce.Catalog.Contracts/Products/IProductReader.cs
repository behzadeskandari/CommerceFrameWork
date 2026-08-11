using Commerce.Catalog.Contracts.Products;
using Commerce.Framework.Core.Results;

namespace Commerce.Catalog.Contracts.Products;

public interface IProductReader
{
    Task<Result<ProductDetailDto>> GetByIdAsync(int productId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ProductSummaryDto>>> ListAsync(
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);
}
