namespace Commerce.Framework.PluginContracts.Settings;

public enum PluginSettingValueType
{
    String = 0,
    Boolean = 1,
    Integer = 2,
    Decimal = 3,
    Secret = 4
}

public sealed record PluginSettingDefinition(
    string Key,
    string Description,
    PluginSettingValueType ValueType,
    string DefaultValue,
    bool IsStoreScoped = true,
    bool IsSecret = false);

public interface IPluginSettingDefinitionProvider
{
    string PluginSystemName { get; }

    IReadOnlyList<PluginSettingDefinition> GetDefinitions();
}
