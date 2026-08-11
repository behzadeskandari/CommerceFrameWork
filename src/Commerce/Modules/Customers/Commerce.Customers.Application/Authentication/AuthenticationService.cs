using Commerce.Customers.Application.Authentication;
using Commerce.Customers.Application.Abstractions;
using Commerce.Customers.Application.Customers;
using Commerce.Framework.Contracts.Security;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Framework.Data.Identity;
using Microsoft.AspNetCore.Identity;

namespace Commerce.Customers.Application.Authentication;

public sealed class AuthenticationService(
    UserManager<CommerceIdentityUser> userManager,
    SignInManager<CommerceIdentityUser> signInManager,
    ICustomerService customerService,
    ICustomerRepository customerRepository) : IAuthenticationService
{
    public async Task<Result<AuthenticationResult>> RegisterAsync(
        RegisterCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Result.Failure<AuthenticationResult>(Error.Validation("Password is required."));
        }

        var existingUser = await userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (existingUser is not null)
        {
            return Result.Failure<AuthenticationResult>(
                Error.Conflict($"A user with email '{request.Email}' already exists."));
        }

        var user = new CommerceIdentityUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            DisplayName = $"{request.FirstName} {request.LastName}".Trim()
        };

        var createResult = await userManager.CreateAsync(user, request.Password).ConfigureAwait(false);
        if (!createResult.Succeeded)
        {
            return Result.Failure<AuthenticationResult>(
                Error.Validation(FormatIdentityErrors(createResult)));
        }

        var roleResult = await userManager.AddToRoleAsync(user, CommerceRoles.Customer).ConfigureAwait(false);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user).ConfigureAwait(false);
            return Result.Failure<AuthenticationResult>(
                Error.Failure(FormatIdentityErrors(roleResult)));
        }

        var customerResult = await customerService.RegisterAsync(
            new CreateCustomerRequest(
                user.Id,
                request.Email,
                request.FirstName,
                request.LastName,
                request.PhoneNumber),
            cancellationToken).ConfigureAwait(false);

        if (!customerResult.IsSuccess)
        {
            await userManager.DeleteAsync(user).ConfigureAwait(false);
            return Result.Failure<AuthenticationResult>(customerResult.Error!);
        }

        await signInManager.SignInAsync(user, isPersistent: false).ConfigureAwait(false);

        return Result.Success(new AuthenticationResult(
            user.Id,
            customerResult.Value!.Id,
            customerResult.Value.Email));
    }

    public async Task<Result<AuthenticationResult>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (user is null)
        {
            return Result.Failure<AuthenticationResult>(Error.Validation("Invalid email or password."));
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true).ConfigureAwait(false);

        if (!signInResult.Succeeded)
        {
            return Result.Failure<AuthenticationResult>(Error.Validation("Invalid email or password."));
        }

        await signInManager.SignInAsync(user, request.RememberMe).ConfigureAwait(false);

        var customer = await customerRepository.GetByIdentityUserIdAsync(user.Id, cancellationToken)
            .ConfigureAwait(false);

        if (customer is null || customer.Deleted)
        {
            return Result.Success(new AuthenticationResult(
                user.Id,
                0,
                user.Email ?? request.Email));
        }

        return Result.Success(new AuthenticationResult(
            user.Id,
            customer.Id,
            customer.Email));
    }

    public async Task<Result> LogoutAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await signInManager.SignOutAsync().ConfigureAwait(false);
        return Result.Success();
    }

    private static string FormatIdentityErrors(IdentityResult result) =>
        string.Join(' ', result.Errors.Select(error => error.Description));
}
