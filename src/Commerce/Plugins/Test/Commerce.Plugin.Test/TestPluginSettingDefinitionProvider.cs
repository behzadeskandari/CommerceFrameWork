using Commerce.Framework.PluginContracts.Settings;

namespace Commerce.Plugin.Test;

public sealed class TestPluginSettingDefinitionProvider : IPluginSettingDefinitionProvider
{
    public string PluginSystemName => "Commerce.Test";

    public IReadOnlyList<PluginSettingDefinition> GetDefinitions() =>
    [
        new("Commerce.Test.SimulateFailure", "Simulate plugin startup failure.", PluginSettingValueType.Boolean, "false"),
        new("Commerce.Test.SecretToken", "Secret token for validation.", PluginSettingValueType.Secret, string.Empty, true, true)
    ];
}
