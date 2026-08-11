using System.Security.Claims;
using Commerce.Framework.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    [HttpGet("session")]
    [AllowAnonymous]
    public IActionResult GetSession()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Ok(new
            {
                success = true,
                data = new SessionResponse(
                    IsAuthenticated: false,
                    IdentityUserId: null,
                    Email: null,
                    CustomerId: null,
                    Roles: Array.Empty<string>(),
                    Permissions: Array.Empty<string>())
            });
        }

        var permissions = User.FindAll(CommerceClaimTypes.Permission)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(permission => permission, StringComparer.Ordinal)
            .ToArray();

        var roles = User.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(role => role, StringComparer.Ordinal)
            .ToArray();

        int? customerId = int.TryParse(User.FindFirstValue(CommerceClaimTypes.CustomerId), out var parsedCustomerId)
            ? parsedCustomerId
            : null;

        return Ok(new
        {
            success = true,
            data = new SessionResponse(
                IsAuthenticated: true,
                IdentityUserId: User.FindFirstValue(ClaimTypes.NameIdentifier),
                Email: User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name,
                CustomerId: customerId,
                Roles: roles,
                Permissions: permissions)
        });
    }
}

public sealed record SessionResponse(
    bool IsAuthenticated,
    string? IdentityUserId,
    string? Email,
    int? CustomerId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
