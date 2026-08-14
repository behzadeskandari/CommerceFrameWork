using Commerce.Framework.Plugins.Loading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Framework.Plugins.Mvc;

public static class PluginMvcExtensions
{
    public static IMvcBuilder AddCommercePluginControllers(this IMvcBuilder mvcBuilder)
    {
        ArgumentNullException.ThrowIfNull(mvcBuilder);

        mvcBuilder.ConfigureApplicationPartManager(manager =>
        {
            var existingNames = manager.ApplicationParts
                .Select(part => part.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var assembly in PluginAssemblyRegistry.Instance.Assemblies.Values)
            {
                if (existingNames.Contains(assembly.GetName().Name ?? assembly.FullName ?? assembly.GetHashCode().ToString()))
                {
                    continue;
                }

                manager.ApplicationParts.Add(new AssemblyPart(assembly));
            }

            manager.FeatureProviders.Add(new PluginControllerFeatureProvider());
        });

        mvcBuilder.AddMvcOptions(options =>
        {
            options.Conventions.Add(new PluginControllerRouteConvention());
        });

        return mvcBuilder;
    }
}
