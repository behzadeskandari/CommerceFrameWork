using Commerce.Framework.Contracts.Security;



namespace Commerce.Tax.Infrastructure.Security;



public static class TaxPermissions

{

    public const string View = "Tax.View";

    public const string Manage = "Tax.Manage";

    public const string Configure = "Tax.Configure";

}



public sealed class TaxPermissionContributor : IModulePermissionContributor

{

    public string ModuleSystemName => "Commerce.Tax";



    public IReadOnlyList<PermissionDefinition> GetPermissions() =>

    [

        new(TaxPermissions.View, "View tax configuration.", ModuleSystemName),

        new(TaxPermissions.Manage, "Manage tax categories, zones, and rates.", ModuleSystemName),

        new(TaxPermissions.Configure, "Configure tax providers and settings.", ModuleSystemName)

    ];

}


