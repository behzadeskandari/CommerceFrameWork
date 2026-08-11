using Commerce.Framework.Contracts.Security;

namespace Commerce.Catalog.Infrastructure.Security;

public sealed class CatalogPermissionContributor : IModulePermissionContributor
{
    public string ModuleSystemName => "Commerce.Catalog";

    public IReadOnlyList<PermissionDefinition> GetPermissions() =>
    [
        new("Catalog.Products.View", "View catalog products.", ModuleSystemName),
        new("Catalog.Products.Create", "Create catalog products.", ModuleSystemName),
        new("Catalog.Products.Update", "Update catalog products.", ModuleSystemName),
        new("Catalog.Products.Delete", "Delete catalog products.", ModuleSystemName),
        new("Catalog.Categories.View", "View catalog categories.", ModuleSystemName),
        new("Catalog.Categories.Create", "Create catalog categories.", ModuleSystemName),
        new("Catalog.Categories.Update", "Update catalog categories.", ModuleSystemName),
        new("Catalog.Categories.Delete", "Delete catalog categories.", ModuleSystemName)
    ];
}
