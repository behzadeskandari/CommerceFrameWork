namespace Commerce.Framework.PluginContracts.Ui;

public interface IPluginUiContributor
{
    string PluginSystemName { get; }

    IReadOnlyList<PluginAdminNavItem> AdminNavItems { get; }
}

public sealed record PluginAdminNavItem(
    string Title,
    string Route,
    string? Icon = null,
    int DisplayOrder = 0);
