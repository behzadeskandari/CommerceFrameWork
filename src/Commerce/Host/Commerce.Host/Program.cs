using Commerce.Framework.Application.DependencyInjection;
using Commerce.Framework.Contracts.Installation;
using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Data.DependencyInjection;
using Commerce.Framework.Infrastructure.DependencyInjection;
using Commerce.Host.Authorization;
using Commerce.Host.Installation;
using Commerce.Host.Middleware;
using Commerce.Modules.Catalog;
using Commerce.Modules.Core;
using Commerce.Modules.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCommerceInfrastructure(builder.Configuration);
builder.Services.AddCommerceModules(builder.Configuration, modules =>
{
    modules.AddModule<CoreModule>();
    modules.AddModule<CustomersModule>();
    modules.AddModule<CatalogModule>();
});
builder.Services.AddCommerceData(builder.Configuration);
builder.Services.AddCommerceModuleRuntime();

builder.Services.AddAuthorization();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

builder.Services.AddControllers();

var app = builder.Build();

await app.Services.LoadPersistedInstallationConfigurationAsync().ConfigureAwait(false);

app.UseMiddleware<InstallationGateMiddleware>();
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
