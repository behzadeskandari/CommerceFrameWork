using Commerce.Framework.Contracts.Installation;
using Commerce.Framework.Contracts.Localization;
using Commerce.Framework.Contracts.Tenancy;
using Commerce.Store.Application.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Commerce.Store.Infrastructure.Middleware;

public sealed class StoreContextMiddleware(
    RequestDelegate next,
    ILogger<StoreContextMiddleware> logger)
{
    public const string LanguageCookieName = "commerce.language";

    public async Task InvokeAsync(
        HttpContext context,
        IInstallationStateService installationStateService,
        IStoreResolver storeResolver,
        ILanguageResolver languageResolver,
        IStoreContextAccessor storeContextAccessor,
        IStoreCurrencyRepository currencyRepository)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/installation", StringComparison.OrdinalIgnoreCase) ||
            !await installationStateService.IsInstalledAsync(context.RequestAborted).ConfigureAwait(false))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var host = context.Request.Host.Host;
        var port = context.Request.Host.Port is > 0 and <= 65535 ? context.Request.Host.Port : null;
        var scheme = context.Request.Scheme;

        var resolution = await storeResolver
            .ResolveAsync(host, port, scheme, context.RequestAborted)
            .ConfigureAwait(false);

        if (resolution is not null)
        {
            storeContextAccessor.SetStore(resolution.StoreId, resolution.SystemName, resolution.Name);

            context.Request.Headers.TryGetValue("Accept-Language", out var acceptLanguage);
            context.Request.Cookies.TryGetValue(LanguageCookieName, out var languageCookie);

            var language = await languageResolver
                .ResolveAsync(
                    resolution.StoreId,
                    resolution.DefaultLanguageId,
                    acceptLanguage.FirstOrDefault(),
                    languageCookie,
                    context.RequestAborted)
                .ConfigureAwait(false);

            if (language is not null)
            {
                storeContextAccessor.SetLanguage(
                    language.LanguageId,
                    language.LanguageCode,
                    language.CultureCode,
                    language.IsRtl);
            }

            var currency = await currencyRepository
                .GetByIdAsync(resolution.DefaultCurrencyId, context.RequestAborted)
                .ConfigureAwait(false);

            if (currency is not null && currency.IsActive)
            {
                storeContextAccessor.SetCurrency(currency.Id, currency.Code);
            }

            logger.LogDebug(
                "Store context resolved: store={StoreId}, language={LanguageCode}, currency={CurrencyCode}.",
                resolution.StoreId,
                language?.LanguageCode,
                currency?.Code);
        }

        await next(context).ConfigureAwait(false);
    }
}

public static class StoreContextApplicationBuilderExtensions
{
    public static IApplicationBuilder UseStoreContext(this IApplicationBuilder app) =>
        app.UseMiddleware<StoreContextMiddleware>();
}
