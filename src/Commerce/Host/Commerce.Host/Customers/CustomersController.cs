using Commerce.Customers.Application.Authentication;
using Commerce.Customers.Application.Customers;
using Commerce.Customers.Contracts.Customers;
using Commerce.Framework.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Commerce.Host.Customers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController(
    IAuthenticationService authenticationService,
    ICustomerService customerService,
    ICurrentCustomerContext currentCustomerContext) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromBody] RegisterCustomerApiRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.RegisterAsync(
            new RegisterCustomerRequest(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                request.PhoneNumber),
            cancellationToken).ConfigureAwait(false);

        return ToActionResult(result, value => value);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginCustomerApiRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.LoginAsync(
            new LoginRequest(request.Email, request.Password, request.RememberMe),
            cancellationToken).ConfigureAwait(false);

        return ToActionResult(result, value => value);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var result = await authenticationService.LogoutAsync(cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
    {
        if (currentCustomerContext.CustomerId is not int customerId)
        {
            return Unauthorized(new { success = false, error = "Customer profile is not available for the current user." });
        }

        var result = await customerService.GetByIdAsync(customerId, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateCurrent(
        [FromBody] UpdateCustomerApiRequest request,
        CancellationToken cancellationToken)
    {
        if (currentCustomerContext.CustomerId is not int customerId)
        {
            return Unauthorized(new { success = false, error = "Customer profile is not available for the current user." });
        }

        var result = await customerService.UpdateAsync(
            customerId,
            new UpdateCustomerRequest(request.FirstName, request.LastName, request.PhoneNumber),
            cancellationToken).ConfigureAwait(false);

        return ToActionResult(result, value => value);
    }

    private IActionResult ToActionResult(Result result)
    {
        if (result.IsSuccess)
        {
            return Ok(new { success = true });
        }

        return MapFailure(result.Error!);
    }

    private IActionResult ToActionResult<T>(Result<T> result, Func<T, object?> dataSelector)
    {
        if (result.IsSuccess)
        {
            return Ok(new { success = true, data = dataSelector(result.Value!) });
        }

        return MapFailure(result.Error!);
    }

    private IActionResult MapFailure(Commerce.Framework.Core.Errors.Error error) =>
        error.Type switch
        {
            Commerce.Framework.Core.Errors.ErrorType.NotFound => NotFound(new { success = false, error = error.Message }),
            Commerce.Framework.Core.Errors.ErrorType.Conflict => Conflict(new { success = false, error = error.Message }),
            Commerce.Framework.Core.Errors.ErrorType.Unauthorized => Unauthorized(new { success = false, error = error.Message }),
            _ => BadRequest(new { success = false, error = error.Message })
        };
}

public sealed record RegisterCustomerApiRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? PhoneNumber = null);

public sealed record LoginCustomerApiRequest(
    string Email,
    string Password,
    bool RememberMe = false);

public sealed record UpdateCustomerApiRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber = null);
