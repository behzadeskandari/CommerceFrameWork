using Commerce.Framework.Core.Results;

namespace Commerce.Customers.Contracts.Customers;

public interface ICustomerAddressReader
{
    Task<Result<CustomerAddressDto>> GetByIdAsync(
        int customerId,
        int addressId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CustomerAddressDto>>> ListAsync(
        int customerId,
        CancellationToken cancellationToken = default);
}
