namespace Commerce.Framework.PluginContracts.Ui;

public sealed record PluginUiMetadataDto(
    string PluginSystemName,
    IReadOnlyList<PluginAdminNavItemDto> AdminNavItems,
    IReadOnlyList<PluginUiContributionDto> Contributions);

public sealed record PluginAdminNavItemDto(
    string Title,
    string Route,
    string? Icon,
    int DisplayOrder,
    string? Permission);

public sealed record PluginUiContributionDto(
    string Target,
    string Title,
    string? Permission,
    string? ConfigurationComponent,
    int DisplayOrder);

public interface IPluginUiMetadataProvider
{
    string PluginSystemName { get; }

    PluginUiMetadataDto GetMetadata();
}
