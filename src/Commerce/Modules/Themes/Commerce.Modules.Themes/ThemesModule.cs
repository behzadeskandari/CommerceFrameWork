using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Migrations;
using Commerce.Framework.Themes;
using Commerce.Themes.Infrastructure.DependencyInjection;
using Commerce.Themes.Infrastructure.Migrations;
using Commerce.Themes.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Modules.Themes;

public sealed class ThemesModule : CommerceModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        Id: "commerce.themes",
        SystemName: "Commerce.Themes",
        Name: "Themes",
        Version: new Version(1, 0, 0),
        Description: "Storefront theme engine and store assignments.",
        Dependencies:
        [
            new ModuleDependency("Commerce.Core"),
            new ModuleDependency("Commerce.Store")
        ],
        IsRequired: false);

    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddCommerceThemes();
        services.AddSingleton<ICommerceModelContributor, ThemesModelContributor>();
        services.AddSingleton<ICommerceMigration, ThemesInitialMigration>();
        services.AddThemesInfrastructure();
    }
}
