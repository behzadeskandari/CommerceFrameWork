namespace Commerce.Customers.Contracts.Customers;

public interface ICustomerRegisteredHandler
{
    Task HandleCustomerRegisteredAsync(int customerId, string email, CancellationToken cancellationToken = default);
}
