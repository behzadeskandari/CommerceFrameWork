using Commerce.Customers.Application.Abstractions;
using Commerce.Framework.Contracts.Installation;
using Commerce.Framework.Contracts.Security;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Framework.Data.Identity;
using Microsoft.AspNetCore.Identity;

namespace Commerce.Customers.Infrastructure.Security;

public sealed class AdministratorProvisioningService(
    UserManager<CommerceIdentityUser> userManager,
    RoleManager<CommerceIdentityRole> roleManager) : IAdministratorProvisioningService
{
    public async Task<Result> CreateAdministratorAsync(
        AdministratorSetupRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return Result.Failure(Error.Validation("Administrator email, username, and password are required."));
        }

        if (request.Password.Length < 8)
        {
            return Result.Failure(Error.Validation("Administrator password must be at least 8 characters."));
        }

        if (await HasAdministratorAsync(cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure(Error.Conflict("An administrator has already been created."));
        }

        var existingUser = await userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (existingUser is not null)
        {
            return Result.Failure(Error.Conflict($"A user with email '{request.Email}' already exists."));
        }

        var user = new CommerceIdentityUser
        {
            UserName = request.Username.Trim(),
            Email = request.Email.Trim(),
            DisplayName = request.Username.Trim(),
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, request.Password).ConfigureAwait(false);
        if (!createResult.Succeeded)
        {
            return Result.Failure(Error.Validation(FormatIdentityErrors(createResult)));
        }

        var roleResult = await userManager.AddToRoleAsync(user, CommerceRoles.Administrator).ConfigureAwait(false);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user).ConfigureAwait(false);
            return Result.Failure(Error.Failure(FormatIdentityErrors(roleResult)));
        }

        return Result.Success();
    }

    public async Task<bool> HasAdministratorAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var role = await roleManager.FindByNameAsync(CommerceRoles.Administrator).ConfigureAwait(false);
        if (role is null)
        {
            return false;
        }

        return (await userManager.GetUsersInRoleAsync(CommerceRoles.Administrator).ConfigureAwait(false)).Count > 0;
    }

    private static string FormatIdentityErrors(IdentityResult result) =>
        string.Join(' ', result.Errors.Select(error => error.Description));
}
