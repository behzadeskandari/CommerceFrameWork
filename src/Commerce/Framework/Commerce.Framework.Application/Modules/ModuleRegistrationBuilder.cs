using Commerce.Framework.Contracts.Modules;

namespace Commerce.Framework.Application.Modules;

public sealed class ModuleRegistrationBuilder
{
    private readonly List<Type> _moduleTypes = [];

    public ModuleRegistrationBuilder AddModule<TModule>()
        where TModule : class, ICommerceModule
    {
        _moduleTypes.Add(typeof(TModule));
        return this;
    }

    public ModuleRegistrationBuilder AddModule(Type moduleType)
    {
        ArgumentNullException.ThrowIfNull(moduleType);

        if (!typeof(ICommerceModule).IsAssignableFrom(moduleType))
        {
            throw new ArgumentException(
                $"Type '{moduleType.FullName}' does not implement {nameof(ICommerceModule)}.",
                nameof(moduleType));
        }

        if (moduleType.IsAbstract)
        {
            throw new ArgumentException(
                $"Module type '{moduleType.FullName}' must be concrete.",
                nameof(moduleType));
        }

        _moduleTypes.Add(moduleType);
        return this;
    }

    public ModuleRegistrationBuilder AddModulesFromAssembly(Type assemblyMarker)
    {
        ArgumentNullException.ThrowIfNull(assemblyMarker);

        var moduleTypes = assemblyMarker.Assembly
            .GetTypes()
            .Where(t => typeof(ICommerceModule).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false });

        foreach (var moduleType in moduleTypes)
        {
            AddModule(moduleType);
        }

        return this;
    }

    internal IReadOnlyList<Type> ModuleTypes => _moduleTypes;
}
