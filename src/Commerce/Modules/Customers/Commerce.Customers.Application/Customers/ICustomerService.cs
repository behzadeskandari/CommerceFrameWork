using Commerce.Customers.Application.Customers;
using Commerce.Customers.Contracts.Customers;
using Commerce.Framework.Core.Results;

namespace Commerce.Customers.Application.Customers;

public interface ICustomerService : ICustomerReader
{
    Task<Result<CustomerDetailDto>> RegisterAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CustomerDetailDto>> UpdateAsync(
        int customerId,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeactivateAsync(int customerId, CancellationToken cancellationToken = default);
}
