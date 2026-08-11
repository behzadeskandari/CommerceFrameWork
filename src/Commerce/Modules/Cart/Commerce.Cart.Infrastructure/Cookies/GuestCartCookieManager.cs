using Commerce.Cart.Application.Abstractions;
using Commerce.Cart.Contracts.Carts;
using Microsoft.AspNetCore.Http;

namespace Commerce.Cart.Infrastructure.Cookies;

public sealed class GuestCartCookieManager(IHttpContextAccessor httpContextAccessor) : IGuestCartCookieManager, IGuestCartContext
{
    public const string CookieName = "commerce.cart.guest";

    public string? GetGuestToken() =>
        httpContextAccessor.HttpContext?.Request.Cookies[CookieName];

    public void SetGuestToken(string token, DateTime expiresAtUtc)
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
        {
            return;
        }

        context.Response.Cookies.Append(
            CookieName,
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = expiresAtUtc,
                IsEssential = true,
                Path = "/"
            });
    }

    public void ClearGuestToken()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
        {
            return;
        }

        context.Response.Cookies.Delete(CookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
    }
}
