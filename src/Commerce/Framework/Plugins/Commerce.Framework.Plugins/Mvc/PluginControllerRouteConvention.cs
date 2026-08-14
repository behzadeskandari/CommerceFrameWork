using System.Reflection;
using Commerce.Framework.PluginContracts.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Commerce.Framework.Plugins.Loading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Commerce.Framework.Plugins.Mvc;

public sealed class PluginControllerRouteConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        ArgumentNullException.ThrowIfNull(controller);

        if (!PluginAssemblyRegistry.Instance.TryGetSystemName(controller.ControllerType.Assembly, out var systemName))
        {
            return;
        }

        var pluginAttribute = controller.Attributes.OfType<PluginControllerAttribute>().FirstOrDefault();
        if (pluginAttribute is not null &&
            !string.Equals(pluginAttribute.PluginSystemName, systemName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Controller '{controller.ControllerType.FullName}' declares plugin '{pluginAttribute.PluginSystemName}' " +
                $"but is loaded from assembly registered as '{systemName}'.");
        }

        var routePrefix = $"api/plugins/{systemName.ToLowerInvariant()}";

        foreach (var selector in controller.Selectors)
        {
            if (selector.AttributeRouteModel is null)
            {
                selector.AttributeRouteModel = new AttributeRouteModel(
                    new RouteAttribute($"{routePrefix}/[controller]"));
                continue;
            }

            var template = selector.AttributeRouteModel.Template ?? string.Empty;
            if (template.StartsWith("api/", StringComparison.OrdinalIgnoreCase) &&
                !template.StartsWith(routePrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Plugin controller '{controller.ControllerType.FullName}' cannot override core routes. " +
                    $"Use relative routes under '{routePrefix}/'.");
            }

            if (!template.StartsWith(routePrefix, StringComparison.OrdinalIgnoreCase))
            {
                selector.AttributeRouteModel = new AttributeRouteModel(
                    new RouteAttribute($"{routePrefix}/{template.TrimStart('/')}"));
            }
        }

        if (controller.Selectors.Count == 0)
        {
            controller.Selectors.Add(new SelectorModel
            {
                AttributeRouteModel = new AttributeRouteModel(new RouteAttribute($"{routePrefix}/[controller]"))
            });
        }
    }
}

public sealed class PluginControllerFeatureProvider : ControllerFeatureProvider
{
    protected override bool IsController(TypeInfo typeInfo)
    {
        if (!base.IsController(typeInfo))
        {
            return false;
        }

        if (!PluginAssemblyRegistry.Instance.TryGetSystemName(typeInfo.Assembly, out _))
        {
            return true;
        }

        return typeInfo.IsPublic && !typeInfo.IsAbstract;
    }
}
