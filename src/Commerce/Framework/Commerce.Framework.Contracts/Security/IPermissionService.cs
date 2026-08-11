using System.Security.Claims;

namespace Commerce.Framework.Contracts.Security;

public interface IPermissionService
{
    IReadOnlyList<PermissionDefinition> GetAllPermissions();

    Task<IReadOnlyList<string>> GetPermissionsForUserAsync(string userId, CancellationToken cancellationToken = default);

    Task<bool> HasPermissionAsync(ClaimsPrincipal principal, string permission, CancellationToken cancellationToken = default);
}
