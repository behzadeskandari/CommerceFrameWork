namespace Commerce.Framework.Contracts.Modules;

public interface ICommerceModuleRegistry
{
    IReadOnlyList<ModuleRuntimeInfo> GetModules();

    ModuleRuntimeInfo? GetModule(string systemName);

    IReadOnlyList<ModuleRuntimeInfo> GetModulesInDependencyOrder();
}
