using Commerce.Framework.Application.Installation;
using Commerce.Framework.Contracts.Modules;
using Commerce.Framework.Contracts.Installation;
using Commerce.Framework.Contracts.Security;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Framework.Data.Configuration;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Entities;
using Commerce.Framework.Data.Migrations;
using Commerce.Framework.Data.Seeding;
using Commerce.Framework.Data.Tenancy;
using Commerce.Framework.Domain.ValueObjects;
using Commerce.Framework.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Commerce.Framework.Data.Installation;

public sealed class InstallationService : IInstallationService
{
    private readonly CommerceDbContext _dbContext;
    private readonly IInstallationConnectionProvider _connectionProvider;
    private readonly IInstallationStateService _stateService;
    private readonly MigrationRunner _migrationRunner;
    private readonly SeederRunner _seederRunner;
    private readonly IPasswordHasher _passwordHasher;
    private readonly InstallationRequirementsEvaluator _requirementsEvaluator;
    private readonly ICommerceDbContextConfigurator _dbContextConfigurator;
    private readonly Microsoft.Extensions.Options.IOptions<CommerceDataOptions> _dataOptions;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ICommerceModuleManager _moduleManager;
    private readonly IStoreContextInitializerService _storeContextInitializer;
    private readonly IAdministratorProvisioningService? _administratorProvisioningService;
    private readonly ILogger<InstallationService> _logger;

    public InstallationService(
        CommerceDbContext dbContext,
        IInstallationConnectionProvider connectionProvider,
        IInstallationStateService stateService,
        MigrationRunner migrationRunner,
        SeederRunner seederRunner,
        IPasswordHasher passwordHasher,
        InstallationRequirementsEvaluator requirementsEvaluator,
        ICommerceDbContextConfigurator dbContextConfigurator,
        Microsoft.Extensions.Options.IOptions<CommerceDataOptions> dataOptions,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ICommerceModuleManager moduleManager,
        IStoreContextInitializerService storeContextInitializer,
        ILogger<InstallationService> logger,
        IAdministratorProvisioningService? administratorProvisioningService = null)
    {
        _dbContext = dbContext;
        _connectionProvider = connectionProvider;
        _stateService = stateService;
        _migrationRunner = migrationRunner;
        _seederRunner = seederRunner;
        _passwordHasher = passwordHasher;
        _requirementsEvaluator = requirementsEvaluator;
        _dbContextConfigurator = dbContextConfigurator;
        _dataOptions = dataOptions;
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _moduleManager = moduleManager;
        _storeContextInitializer = storeContextInitializer;
        _administratorProvisioningService = administratorProvisioningService;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<RequirementCheckResult>>> ValidateRequirementsAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureNotLockedAsync(cancellationToken).ConfigureAwait(false);

        var results = _requirementsEvaluator.Evaluate(_configuration, _hostEnvironment.ContentRootPath);
        _logger.LogInformation("Installation requirements validated.");

        if (results.All(x => x.IsSatisfied))
        {
            return Result.Success(results);
        }

        return Result.Failure<IReadOnlyList<RequirementCheckResult>>(
            Error.Validation("One or more installation requirements failed."));
    }

    public async Task<Result> ConfigureDatabaseAsync(
        DatabaseSetupRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureNotLockedAsync(cancellationToken).ConfigureAwait(false);
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ConnectionString))
        {
            return Result.Failure(Error.Validation("Connection string is required."));
        }

        if (!TryParseProvider(request.Provider, out var provider))
        {
            return Result.Failure(Error.Validation($"Unsupported database provider '{request.Provider}'."));
        }

        _connectionProvider.SetPending(provider, request.ConnectionString.Trim());

        try
        {
            var effectiveOptions = new CommerceDataOptions
            {
                Provider = provider,
                ConnectionString = request.ConnectionString.Trim(),
                CommandTimeoutSeconds = _dataOptions.Value.CommandTimeoutSeconds
            };

            var optionsBuilder = new DbContextOptionsBuilder<CommerceDbContext>();
            _dbContextConfigurator.Configure(optionsBuilder, effectiveOptions);
            optionsBuilder.ReplaceService<IModelCacheKeyFactory, CommerceModelCacheKeyFactory>();

            await using var probeContext = new CommerceDbContext(
                optionsBuilder.Options,
                new ServiceCollection().BuildServiceProvider());
            var canConnect = await probeContext.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false);
            if (!canConnect)
            {
                return Result.Failure(Error.Validation("Unable to connect to the database."));
            }

            await _connectionProvider.PersistAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation(
                "Database configured for provider {Provider}. Connection={Connection}",
                provider,
                SensitiveValueMasker.MaskConnectionString(request.ConnectionString));

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database configuration failed.");
            return Result.Failure(Error.Validation("Database connection failed. Verify server, credentials, and permissions."));
        }
    }

    public async Task<Result<int>> RunMigrationsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureNotLockedAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Installation migrations started.");
        return await _migrationRunner.RunPendingMigrationsAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result> RunSeedAsync(CancellationToken cancellationToken = default)
    {
        await EnsureNotLockedAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            _logger.LogInformation("Installation seed started.");
            await _seederRunner.RunAsync(_dbContext, cancellationToken).ConfigureAwait(false);
            await UpdateInstallationStepAsync(InstallationStep.Seed, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Installation seed failed.");
            return Result.Failure(Error.Failure("Seed operation failed."));
        }
    }

    public async Task<Result> CreateAdministratorAsync(
        AdministratorSetupRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureNotLockedAsync(cancellationToken).ConfigureAwait(false);
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return Result.Failure(Error.Validation("Administrator email, username, and password are required."));
        }

        if (request.Password.Length < 8)
        {
            return Result.Failure(Error.Validation("Administrator password must be at least 8 characters."));
        }

        if (_administratorProvisioningService is not null)
        {
            var provisioningResult = await _administratorProvisioningService
                .CreateAdministratorAsync(request, cancellationToken)
                .ConfigureAwait(false);

            if (provisioningResult.IsSuccess)
            {
                await UpdateInstallationStepAsync(InstallationStep.Administrator, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Administrator created for {Username}.", request.Username);
            }

            return provisioningResult;
        }

        if (await _dbContext.BootstrapAdministrators.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure(Error.Conflict("An administrator has already been created."));
        }

        var hash = _passwordHasher.HashPassword(request.Password);
        _dbContext.BootstrapAdministrators.Add(new BootstrapAdministrator
        {
            Email = request.Email.Trim(),
            Username = request.Username.Trim(),
            PasswordHash = hash,
            IsActive = true,
            CreatedOnUtc = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await UpdateInstallationStepAsync(InstallationStep.Administrator, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Bootstrap administrator created for {Username}.", request.Username);

        return Result.Success();
    }

    public async Task<Result> CreateStoreAsync(
        StoreSetupRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureNotLockedAsync(cancellationToken).ConfigureAwait(false);
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Url))
        {
            return Result.Failure(Error.Validation("Store name and URL are required."));
        }

        if (await _dbContext.BootstrapStores.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure(Error.Conflict("A store has already been created."));
        }

        _dbContext.BootstrapStores.Add(new BootstrapStore
        {
            Name = request.Name.Trim(),
            Url = request.Url.Trim(),
            Hosts = string.IsNullOrWhiteSpace(request.Hosts) ? null : request.Hosts.Trim(),
            IsActive = true
        });

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await UpdateInstallationStepAsync(InstallationStep.Store, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Bootstrap store created: {StoreName}.", request.Name);

        return Result.Success();
    }

    public async Task<Result> ConfigureLanguageAsync(
        LanguageSetupRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureNotLockedAsync(cancellationToken).ConfigureAwait(false);
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Culture))
        {
            return Result.Failure(Error.Validation("Language name and culture are required."));
        }

        var existing = await _dbContext.BootstrapLanguages
            .FirstOrDefaultAsync(x => x.Culture == request.Culture.Trim(), cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            return Result.Failure(Error.Conflict("The specified language culture already exists."));
        }

        if (request.IsDefault)
        {
            var defaults = await _dbContext.BootstrapLanguages
                .Where(x => x.IsDefault)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var language in defaults)
            {
                language.IsDefault = false;
            }
        }

        _dbContext.BootstrapLanguages.Add(new BootstrapLanguage
        {
            Name = request.Name.Trim(),
            Culture = request.Culture.Trim(),
            Rtl = request.Rtl,
            IsDefault = request.IsDefault,
            IsPublished = true
        });

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await UpdateInstallationStepAsync(InstallationStep.Language, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Bootstrap language configured: {Culture}.", request.Culture);

        return Result.Success();
    }

    public async Task<Result> ConfigureCurrencyAsync(
        CurrencySetupRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureNotLockedAsync(cancellationToken).ConfigureAwait(false);
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.CurrencyCode))
        {
            return Result.Failure(Error.Validation("Currency name and code are required."));
        }

        try
        {
            _ = Currency.FromCode(request.CurrencyCode);
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Validation(ex.Message));
        }

        if (request.Rate < 0)
        {
            return Result.Failure(Error.Validation("Currency rate cannot be negative."));
        }

        var code = request.CurrencyCode.Trim().ToUpperInvariant();
        if (await _dbContext.BootstrapCurrencies.AnyAsync(x => x.CurrencyCode == code, cancellationToken)
                .ConfigureAwait(false))
        {
            return Result.Failure(Error.Conflict("The specified currency already exists."));
        }

        if (request.IsPrimary)
        {
            var primaries = await _dbContext.BootstrapCurrencies
                .Where(x => x.IsPrimary)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var currency in primaries)
            {
                currency.IsPrimary = false;
            }
        }

        _dbContext.BootstrapCurrencies.Add(new BootstrapCurrency
        {
            Name = request.Name.Trim(),
            CurrencyCode = code,
            Rate = request.Rate,
            IsPrimary = request.IsPrimary,
            IsPublished = true
        });

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await UpdateInstallationStepAsync(InstallationStep.Currency, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Bootstrap currency configured: {CurrencyCode}.", code);

        return Result.Success();
    }

    public async Task<Result> CompleteInstallationAsync(CancellationToken cancellationToken = default)
    {
        await EnsureNotLockedAsync(cancellationToken).ConfigureAwait(false);

        var hasAdministrator = _administratorProvisioningService is not null
            ? await _administratorProvisioningService.HasAdministratorAsync(cancellationToken).ConfigureAwait(false)
            : await _dbContext.BootstrapAdministrators.AnyAsync(cancellationToken).ConfigureAwait(false);

        if (!hasAdministrator)
        {
            return Result.Failure(Error.Validation("Administrator must be created before completing installation."));
        }

        if (!await _dbContext.BootstrapStores.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure(Error.Validation("Store must be created before completing installation."));
        }

        if (!await _dbContext.BootstrapLanguages.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure(Error.Validation("Language must be configured before completing installation."));
        }

        if (!await _dbContext.BootstrapCurrencies.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure(Error.Validation("Currency must be configured before completing installation."));
        }

        var installation = await _dbContext.CommerceInstallations
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (installation is null)
        {
            installation = new CommerceInstallation
            {
                InstallationId = Guid.NewGuid(),
                ApplicationVersion = typeof(InstallationService).Assembly.GetName().Version?.ToString() ?? "1.0.0"
            };
            _dbContext.CommerceInstallations.Add(installation);
        }

        installation.Status = nameof(InstallationStatus.Installed);
        installation.CurrentStep = (int)InstallationStep.Complete;
        installation.InstalledAtUtc = DateTime.UtcNow;
        installation.InstalledVersion = installation.ApplicationVersion;
        installation.LastError = null;

        await UpsertSettingAsync("Commerce.Installation.Completed", "true", cancellationToken)
            .ConfigureAwait(false);

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Commerce installation completed.");

        await _storeContextInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        _moduleManager.RegisterModules();
        await _moduleManager.InitializeModulesAsync(cancellationToken).ConfigureAwait(false);
        await _moduleManager.StartModulesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Commerce module runtime started after installation.");

        return Result.Success();
    }

    private async Task EnsureNotLockedAsync(CancellationToken cancellationToken)
    {
        if (await _stateService.IsInstallationLockedAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Installation is locked because the application is already installed.");
        }
    }

    private async Task UpdateInstallationStepAsync(InstallationStep step, CancellationToken cancellationToken)
    {
        var installation = await _dbContext.CommerceInstallations
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (installation is null)
        {
            installation = new CommerceInstallation
            {
                InstallationId = Guid.NewGuid(),
                Status = nameof(InstallationStatus.InProgress),
                ApplicationVersion = typeof(InstallationService).Assembly.GetName().Version?.ToString() ?? "1.0.0"
            };
            _dbContext.CommerceInstallations.Add(installation);
        }
        else
        {
            installation.Status = nameof(InstallationStatus.InProgress);
        }

        installation.CurrentStep = (int)step;
        installation.LastError = null;
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task UpsertSettingAsync(string name, string value, CancellationToken cancellationToken)
    {
        var setting = await _dbContext.Settings
            .FirstOrDefaultAsync(x => x.Name == name && x.StoreId == 0, cancellationToken)
            .ConfigureAwait(false);

        if (setting is null)
        {
            _dbContext.Settings.Add(new Setting { Name = name, Value = value, StoreId = 0 });
            return;
        }

        setting.Value = value;
    }

    private static bool TryParseProvider(string provider, out CommerceDatabaseProvider parsed)
    {
        if (Enum.TryParse<CommerceDatabaseProvider>(provider, true, out parsed))
        {
            return parsed != CommerceDatabaseProvider.PostgreSql;
        }

        return provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)
            && Enum.TryParse("SqlServer", true, out parsed);
    }
}
