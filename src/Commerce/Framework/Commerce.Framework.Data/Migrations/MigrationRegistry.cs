using Commerce.Framework.Data.Migrations;

namespace Commerce.Framework.Data.Migrations;

public sealed class MigrationRegistry
{
    private readonly IReadOnlyList<ICommerceMigration> _migrations;

    public MigrationRegistry(IEnumerable<ICommerceMigration> migrations)
        : this(migrations, null)
    {
    }

    public MigrationRegistry(IEnumerable<ICommerceMigration> migrations, IReadOnlyList<string>? moduleOrder)
    {
        ArgumentNullException.ThrowIfNull(migrations);

        var materialized = migrations.ToList();
        ValidateNoDuplicates(materialized);

        var moduleIndex = BuildModuleIndex(moduleOrder);

        _migrations = materialized
            .OrderBy(m => moduleIndex.GetValueOrDefault(m.Module, int.MaxValue))
            .ThenBy(m => ParseVersion(m.Version))
            .ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<ICommerceMigration> GetAll() => _migrations;

    public IReadOnlyList<ICommerceMigration> GetOrdered() => _migrations;

    private static void ValidateNoDuplicates(IReadOnlyList<ICommerceMigration> migrations)
    {
        var duplicateVersion = migrations
            .GroupBy(m => (Module: m.Module.ToUpperInvariant(), Version: m.Version))
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateVersion is not null)
        {
            var sample = duplicateVersion.First();
            throw new InvalidOperationException(
                $"Duplicate migration version '{sample.Version}' detected for module '{sample.Module}'.");
        }

        var duplicateName = migrations
            .GroupBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateName is not null)
        {
            throw new InvalidOperationException(
                $"Duplicate migration name '{duplicateName.Key}' detected.");
        }
    }

    internal static Version ParseVersion(string version)
    {
        if (Version.TryParse(version, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Migration version '{version}' is not a valid semantic version.");
    }

    private static Dictionary<string, int> BuildModuleIndex(IReadOnlyList<string>? moduleOrder)
    {
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Core"] = int.MinValue
        };

        if (moduleOrder is null || moduleOrder.Count == 0)
        {
            return index;
        }

        foreach (var (name, position) in moduleOrder.Select((name, position) => (name, position)))
        {
            index[name] = position;
        }

        return index;
    }
}
