using Microsoft.AspNetCore.Authorization;

namespace Commerce.Host.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
