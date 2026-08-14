using Commerce.Framework.PluginContracts.Ui;

namespace Commerce.Plugin.Test;

public sealed class TestPluginUiMetadataProvider : IPluginUiMetadataProvider
{
    public string PluginSystemName => "Commerce.Test";

    public PluginUiMetadataDto GetMetadata() =>
        new(
            PluginSystemName,
            [
                new PluginAdminNavItemDto(
                    "Test Plugin",
                    "/plugins/Commerce.Test",
                    "science",
                    900,
                    "Commerce.Test.View")
            ],
            [
                new PluginUiContributionDto(
                    "PluginConfiguration",
                    "Test Plugin Settings",
                    "Commerce.Test.Configure",
                    "generic-plugin-settings",
                    100)
            ]);
}
