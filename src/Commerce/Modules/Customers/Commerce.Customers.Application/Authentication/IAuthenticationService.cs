using Commerce.Customers.Application.Customers;
using Commerce.Framework.Core.Results;

namespace Commerce.Customers.Application.Authentication;

public sealed record AuthenticationResult(
    string IdentityUserId,
    int CustomerId,
    string Email);

public interface IAuthenticationService
{
    Task<Result<AuthenticationResult>> RegisterAsync(
        RegisterCustomerRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AuthenticationResult>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> LogoutAsync(CancellationToken cancellationToken = default);
}
