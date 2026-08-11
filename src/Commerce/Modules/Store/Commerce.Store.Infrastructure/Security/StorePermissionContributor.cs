using Commerce.Framework.Contracts.Security;

namespace Commerce.Store.Infrastructure.Security;

public static class StorePermissions
{
    public const string StoresView = "Stores.View";
    public const string StoresCreate = "Stores.Create";
    public const string StoresUpdate = "Stores.Update";
    public const string StoresDelete = "Stores.Delete";

    public const string LanguagesView = "Languages.View";
    public const string LanguagesCreate = "Languages.Create";
    public const string LanguagesUpdate = "Languages.Update";
    public const string LanguagesDelete = "Languages.Delete";

    public const string CurrenciesView = "Currencies.View";
    public const string CurrenciesCreate = "Currencies.Create";
    public const string CurrenciesUpdate = "Currencies.Update";
    public const string CurrenciesDelete = "Currencies.Delete";

    public const string SettingsView = "Settings.View";
    public const string SettingsUpdate = "Settings.Update";
}

public sealed class StorePermissionContributor : IModulePermissionContributor
{
    public string ModuleSystemName => "Commerce.Store";

    public IReadOnlyList<PermissionDefinition> GetPermissions() =>
    [
        new(StorePermissions.StoresView, "View stores.", ModuleSystemName),
        new(StorePermissions.StoresCreate, "Create stores.", ModuleSystemName),
        new(StorePermissions.StoresUpdate, "Update stores.", ModuleSystemName),
        new(StorePermissions.StoresDelete, "Delete stores.", ModuleSystemName),
        new(StorePermissions.LanguagesView, "View languages.", ModuleSystemName),
        new(StorePermissions.LanguagesCreate, "Create languages.", ModuleSystemName),
        new(StorePermissions.LanguagesUpdate, "Update languages.", ModuleSystemName),
        new(StorePermissions.LanguagesDelete, "Delete languages.", ModuleSystemName),
        new(StorePermissions.CurrenciesView, "View currencies.", ModuleSystemName),
        new(StorePermissions.CurrenciesCreate, "Create currencies.", ModuleSystemName),
        new(StorePermissions.CurrenciesUpdate, "Update currencies.", ModuleSystemName),
        new(StorePermissions.CurrenciesDelete, "Delete currencies.", ModuleSystemName),
        new(StorePermissions.SettingsView, "View settings.", ModuleSystemName),
        new(StorePermissions.SettingsUpdate, "Update settings.", ModuleSystemName)
    ];
}
