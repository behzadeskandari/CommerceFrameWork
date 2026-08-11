using Commerce.Framework.Contracts.Security;
using Microsoft.AspNetCore.Authorization;

namespace Commerce.Host.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute(string permission) : AuthorizeAttribute(CommercePolicies.ForPermission(permission));
