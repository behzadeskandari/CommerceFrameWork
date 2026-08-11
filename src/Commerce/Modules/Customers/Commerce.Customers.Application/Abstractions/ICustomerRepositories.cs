using Commerce.Customers.Domain.Entities;

namespace Commerce.Customers.Application.Abstractions;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Customer?> GetByIdentityUserIdAsync(string identityUserId, CancellationToken cancellationToken = default);

    Task<Customer?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Customer>> ListAsync(bool includeDeleted, CancellationToken cancellationToken = default);

    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);

    Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default);
}

public interface ICustomerAddressRepository
{
    Task<CustomerAddress?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerAddress>> ListByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);

    Task AddAsync(CustomerAddress address, CancellationToken cancellationToken = default);

    Task UpdateAsync(CustomerAddress address, CancellationToken cancellationToken = default);

    Task DeleteAsync(CustomerAddress address, CancellationToken cancellationToken = default);
}
