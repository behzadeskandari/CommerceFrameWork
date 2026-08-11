using System.Security.Claims;
using Commerce.Customers.Contracts.Customers;
using Commerce.Framework.Contracts.Security;
using Microsoft.AspNetCore.Http;

namespace Commerce.Customers.Infrastructure.Identity;

public sealed class CurrentCustomerContext(IHttpContextAccessor httpContextAccessor) : ICurrentCustomerContext
{
    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public string? IdentityUserId =>
        httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public int? CustomerId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User?.FindFirstValue(CommerceClaimTypes.CustomerId);
            return int.TryParse(value, out var customerId) ? customerId : null;
        }
    }
}
