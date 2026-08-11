using Commerce.Framework.Contracts.Security;

namespace Commerce.Customers.Infrastructure.Security;

public static class CustomersPermissions
{
    public const string View = "Customers.View";

    public const string Update = "Customers.Update";
}

public sealed class CustomersPermissionContributor : IModulePermissionContributor
{
    public string ModuleSystemName => "Commerce.Customers";

    public IReadOnlyList<PermissionDefinition> GetPermissions() =>
    [
        new(CustomersPermissions.View, "View customers.", ModuleSystemName),
        new(CustomersPermissions.Update, "Update customers.", ModuleSystemName)
    ];
}
