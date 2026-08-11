namespace Commerce.Framework.Contracts.Modules;

public interface ICommerceModuleManager
{
    IReadOnlyList<ModuleRuntimeInfo> DiscoverModules();

    void ValidateModules();

    IReadOnlyList<ModuleDescriptor> ResolveDependencies();

    void RegisterModules();

    Task InitializeModulesAsync(CancellationToken cancellationToken = default);

    Task StartModulesAsync(CancellationToken cancellationToken = default);

    Task StopModulesAsync(CancellationToken cancellationToken = default);
}
