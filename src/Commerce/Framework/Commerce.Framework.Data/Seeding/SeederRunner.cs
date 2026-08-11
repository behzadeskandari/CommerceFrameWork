using Commerce.Framework.Application.Modules;
using Commerce.Framework.Contracts.Seeding;
using Commerce.Framework.Data.Db;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Commerce.Framework.Data.Seeding;

public sealed class SeederRunner
{
    private readonly IReadOnlyList<ICommerceSeeder> _seeders;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SeederRunner> _logger;

    public SeederRunner(
        IEnumerable<ICommerceSeeder> seeders,
        IServiceProvider serviceProvider,
        ILogger<SeederRunner> logger)
        : this(seeders, serviceProvider, null, logger)
    {
    }

    public SeederRunner(
        IEnumerable<ICommerceSeeder> seeders,
        IServiceProvider serviceProvider,
        ModuleRegistrationContext? moduleContext,
        ILogger<SeederRunner> logger)
    {
        var moduleIndex = BuildModuleIndex(moduleContext?.OrderedSystemNames);
        _seeders = seeders
            .OrderBy(s => GetModuleIndex(s, moduleIndex))
            .ThenBy(s => s.Order)
            .ThenBy(s => s.Name, StringComparer.Ordinal)
            .ToList();
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RunAsync(CommerceDbContext dbContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        var context = new SeederContext { Services = _serviceProvider };

        foreach (var seeder in _seeders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogInformation("Running seeder {SeederName}", seeder.Name);
            await seeder.SeedAsync(context, cancellationToken).ConfigureAwait(false);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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

    private static int GetModuleIndex(ICommerceSeeder seeder, IReadOnlyDictionary<string, int> moduleIndex)
    {
        if (seeder is IModuleSeeder moduleSeeder)
        {
            return moduleIndex.GetValueOrDefault(moduleSeeder.ModuleSystemName, int.MaxValue);
        }

        return moduleIndex.GetValueOrDefault("Core", 0);
    }
}
