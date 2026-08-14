using Commerce.Framework.Application.DependencyInjection;
using Commerce.Framework.Contracts.Installation;
using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Data.DependencyInjection;
using Commerce.Framework.Infrastructure.DependencyInjection;
using Commerce.Framework.Plugins.DependencyInjection;
using Commerce.Framework.Plugins.Mvc;
using Commerce.Framework.Plugins.StaticFiles;
using Commerce.Host.Authorization;
using Commerce.Host.Configuration;
using Commerce.Host.Installation;
using Commerce.Host.Middleware;
using Commerce.Modules.Cart;
using Commerce.Modules.Catalog;
using Commerce.Modules.Checkout;
using Commerce.Modules.Core;
using Commerce.Modules.Customers;
using Commerce.Modules.Media;
using Commerce.Modules.Inventory;
using Commerce.Modules.Orders;
using Commerce.Modules.Payments;
using Commerce.Modules.Pricing;
using Commerce.Modules.Shipping;
using Commerce.Modules.Downloads;
using Commerce.Modules.Cms;
using Commerce.Modules.Search;
using Commerce.Modules.Reviews;
using Commerce.Modules.Promotions;
using Commerce.Modules.Seo;
using Commerce.Modules.Notifications;
using Commerce.Modules.Scheduling;
using Commerce.Modules.Themes;
using Commerce.Modules.Tax;
using Commerce.Modules.Store;
using Commerce.Modules.Integration;
using Commerce.Modules.Analytics;
using Commerce.Modules.Audit;
using Commerce.Modules.Observability;
using Commerce.Modules.Cache;
using Commerce.Modules.DisasterRecovery;
using Commerce.Modules.SmartstoreImport;
using Commerce.Audit.Infrastructure.DependencyInjection;
using Commerce.Observability.Infrastructure.DependencyInjection;
using Commerce.Cache.Infrastructure.DependencyInjection;
using Commerce.Host.Integration;
using Commerce.Store.Infrastructure.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCommerceInfrastructure(builder.Configuration);
builder.Services.AddCommercePlugins(builder.Configuration, builder.Environment);
builder.Services.AddCommerceModules(builder.Configuration, modules =>
{
    modules.AddModule<CoreModule>();
    modules.AddModule<CustomersModule>();
    modules.AddModule<InventoryModule>();
    modules.AddModule<CatalogModule>();
    modules.AddModule<MediaModule>();
    modules.AddModule<CartModule>();
    modules.AddModule<CheckoutModule>();
    modules.AddModule<PricingModule>();
    modules.AddModule<ShippingModule>();
    modules.AddModule<TaxModule>();
    modules.AddModule<PaymentsModule>();
    modules.AddModule<OrdersModule>();
    modules.AddModule<DownloadsModule>();
    modules.AddModule<CmsModule>();
    modules.AddModule<SearchModule>();
    modules.AddModule<ReviewsModule>();
    modules.AddModule<PromotionsModule>();
    modules.AddModule<SeoModule>();
    modules.AddModule<SchedulingModule>();
    modules.AddModule<NotificationsModule>();
    modules.AddModule<ThemesModule>();
    modules.AddModule<StoreModule>();
    modules.AddModule<IntegrationModule>();
    modules.AddModule<AnalyticsModule>();
    modules.AddModule<AuditModule>();
    modules.AddModule<ObservabilityModule>();
    modules.AddModule<CacheModule>();
    modules.AddModule<DisasterRecoveryModule>();
    modules.AddModule<SmartstoreImportModule>();
});
builder.Services.AddCommerceData(builder.Configuration);
builder.Services.RegisterEnabledPluginServices(builder.Configuration, builder.Environment);
builder.Services.AddCommerceModuleRuntime();
builder.Services.AddCommercePluginRuntime();

builder.Services.AddAuthorization(options =>
{
    options.InvokeHandlersAfterFailure = true;
});
builder.Services.AddScoped<IAuthorizationHandler, AuditingPermissionAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var path = httpContext.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/api/admin", StringComparison.OrdinalIgnoreCase))
        {
            var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 300,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
        }

        return RateLimitPartition.GetNoLimiter("default");
    });
    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = 20;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("admin", limiterOptions =>
    {
        limiterOptions.PermitLimit = 300;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });
});

builder.Services.AddControllers()
    .AddCommercePluginControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

var corsOptions = builder.Configuration
    .GetSection(CorsOptions.SectionName)
    .Get<CorsOptions>() ?? new CorsOptions();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CommerceFrontend", policy =>
    {
        policy.WithOrigins(corsOptions.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

await app.Services.LoadPersistedInstallationConfigurationAsync().ConfigureAwait(false);

app.UseMiddleware<InstallationGateMiddleware>();
app.UseCommerceCorrelation();
app.UseCommerceRequestLogging();
app.UseCommerceSecurityHeaders();
app.UseCommerceOutputCache();
app.UseStoreContext();
app.UseStaticFiles();
app.UseCors("CommerceFrontend");
app.UseRateLimiter();
app.UseApiKeyAuthentication();
app.UseAuthentication();
app.UseAuthorization();
app.UseCommerceAdminAudit();
app.UsePluginStaticFiles();
app.MapControllers();

app.MapCommerceHealthChecks();

app.MapGet("/", async (IInstallationStateService stateService, CancellationToken cancellationToken) =>
{
    if (await stateService.IsInstalledAsync(cancellationToken).ConfigureAwait(false))
    {
        return Results.Ok(new { status = "installed", message = "Commerce is installed and running." });
    }

    return Results.Redirect("/installation");
});

app.MapGet("/modules", (ICommerceModuleRegistry moduleRegistry) =>
{
    var modules = moduleRegistry.GetModulesInDependencyOrder()
        .Select(module => new
        {
            module.Descriptor.SystemName,
            module.Descriptor.Name,
            Version = module.Descriptor.Version.ToString(),
            State = module.State.ToString(),
            Dependencies = module.Descriptor.Dependencies.Select(d => d.ModuleSystemName).ToArray(),
            module.StartupDuration,
            module.FailureReason
        });

    return Results.Ok(new { modules });
});

app.Run();

public partial class Program;
