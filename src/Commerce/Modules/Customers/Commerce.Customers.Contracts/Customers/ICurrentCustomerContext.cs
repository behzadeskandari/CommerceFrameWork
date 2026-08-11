namespace Commerce.Customers.Contracts.Customers;

public interface ICurrentCustomerContext
{
    bool IsAuthenticated { get; }

    string? IdentityUserId { get; }

    int? CustomerId { get; }
}
