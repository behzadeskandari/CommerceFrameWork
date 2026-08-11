using Commerce.Framework.Application.DependencyInjection;
using Commerce.Framework.Application.Modules;
using Commerce.Framework.Contracts.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Framework.Application.DependencyInjection;

public static class ModuleServiceExtensions
{
    public const string DisabledModulesSection = "Commerce:Modules:Disabled";

    public static IServiceCollection AddCommerceModules(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<ModuleRegistrationBuilder> configureModules)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configureModules);

        var builder = new ModuleRegistrationBuilder();
        configureModules(builder);

        var moduleInstances = builder.ModuleTypes
            .Select(type => (ICommerceModule)Activator.CreateInstance(type)!)
            .ToList();

        var disabledSystemNames = configuration
            .GetSection(DisabledModulesSection)
            .Get<string[]>()?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var descriptors = moduleInstances.Select(m => m.Descriptor).ToList();
        var orderedDescriptors = ModuleDependencyResolver.Resolve(descriptors, disabledSystemNames);

        var orderedModules = orderedDescriptors
            .Select(descriptor => moduleInstances.Single(m =>
                string.Equals(m.Descriptor.SystemName, descriptor.SystemName, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var context = new ModuleRegistrationContext(orderedModules, orderedDescriptors, disabledSystemNames);

        foreach (var module in context.Modules)
        {
            if (disabledSystemNames.Contains(module.Descriptor.SystemName))
            {
                context.GetEntry(module.Descriptor.SystemName).State = ModuleState.Disabled;
                continue;
            }

            module.RegisterServices(services, configuration);
            services.AddSingleton(module.GetType(), module);
        }

        services.AddSingleton(context);
        services.AddSingleton<ICommerceModuleRegistry, CommerceModuleRegistry>();
        services.AddSingleton<ICommerceModuleManager, CommerceModuleManager>();

        return services;
    }

    public static IServiceCollection AddCommerceModuleRuntime(this IServiceCollection services)
    {
        services.AddHostedService<ModuleStartupHostedService>();
        return services;
    }
}
