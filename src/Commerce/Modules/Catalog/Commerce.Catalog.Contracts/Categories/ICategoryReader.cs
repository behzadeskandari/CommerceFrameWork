using Commerce.Catalog.Contracts.Categories;
using Commerce.Framework.Core.Results;

namespace Commerce.Catalog.Contracts.Categories;

public interface ICategoryReader
{
    Task<Result<CategoryDetailDto>> GetByIdAsync(int categoryId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CategorySummaryDto>>> ListAsync(CancellationToken cancellationToken = default);
}
