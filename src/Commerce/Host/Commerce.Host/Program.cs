using Commerce.Framework.Application.DependencyInjection;
using Commerce.Framework.Contracts.Installation;
using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Data.DependencyInjection;
using Commerce.Framework.Infrastructure.DependencyInjection;
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
using Commerce.Modules.Store;
using Commerce.Store.Infrastructure.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCommerceInfrastructure(builder.Configuration);
builder.Services.AddCommerceModules(builder.Configuration, modules =>
{
    modules.AddModule<CoreModule>();
    modules.AddModule<CustomersModule>();
    modules.AddModule<InventoryModule>();
    modules.AddModule<CatalogModule>();
    modules.AddModule<MediaModule>();
    modules.AddModule<CartModule>();
    modules.AddModule<CheckoutModule>();
    modules.AddModule<OrdersModule>();
    modules.AddModule<StoreModule>();
});
builder.Services.AddCommerceData(builder.Configuration);
builder.Services.AddCommerceModuleRuntime();

builder.Services.AddAuthorization();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

builder.Services.AddControllers()
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
app.UseStoreContext();
app.UseCors("CommerceFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

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
