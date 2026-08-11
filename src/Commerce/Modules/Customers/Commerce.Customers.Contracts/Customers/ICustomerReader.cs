using Commerce.Framework.Core.Results;

namespace Commerce.Customers.Contracts.Customers;

public interface ICustomerReader
{
    Task<Result<CustomerDetailDto>> GetByIdAsync(int customerId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CustomerSummaryDto>>> ListAsync(
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);
}
