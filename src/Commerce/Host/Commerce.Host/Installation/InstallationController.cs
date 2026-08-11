using Commerce.Framework.Contracts.Installation;
using Commerce.Framework.Core.Results;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Installation;

[ApiController]
[Route("installation")]
public sealed class InstallationController(IInstallationService installationService, IInstallationStateService stateService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        if (await stateService.IsInstallationLockedAsync(cancellationToken).ConfigureAwait(false))
        {
            return Conflict(new { message = "Commerce is already installed." });
        }

        var state = await stateService.GetStateAsync(cancellationToken).ConfigureAwait(false);
        return Ok(state);
    }

    [HttpPost("requirements")]
    public async Task<IActionResult> ValidateRequirements(CancellationToken cancellationToken)
    {
        var result = await installationService.ValidateRequirementsAsync(cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
    }

    [HttpPost("database")]
    public async Task<IActionResult> ConfigureDatabase(
        [FromBody] DatabaseSetupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await installationService.ConfigureDatabaseAsync(request, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }

    [HttpPost("migrate")]
    public async Task<IActionResult> Migrate(CancellationToken cancellationToken)
    {
        var result = await installationService.RunMigrationsAsync(cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
    }

    [HttpPost("seed")]
    public async Task<IActionResult> Seed(CancellationToken cancellationToken)
    {
        var result = await installationService.RunSeedAsync(cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }

    [HttpPost("admin")]
    public async Task<IActionResult> CreateAdministrator(
        [FromBody] AdministratorSetupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await installationService.CreateAdministratorAsync(request, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }

    [HttpPost("store")]
    public async Task<IActionResult> CreateStore(
        [FromBody] StoreSetupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await installationService.CreateStoreAsync(request, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }

    [HttpPost("language")]
    public async Task<IActionResult> ConfigureLanguage(
        [FromBody] LanguageSetupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await installationService.ConfigureLanguageAsync(request, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }

    [HttpPost("currency")]
    public async Task<IActionResult> ConfigureCurrency(
        [FromBody] CurrencySetupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await installationService.ConfigureCurrencyAsync(request, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }

    [HttpPost("complete")]
    public async Task<IActionResult> Complete(CancellationToken cancellationToken)
    {
        var result = await installationService.CompleteInstallationAsync(cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult(Result result)
    {
        if (result.IsSuccess)
        {
            return Ok(new { success = true });
        }

        return BadRequest(new { success = false, error = result.Error?.Message });
    }

    private IActionResult ToActionResult<T>(Result<T> result, Func<T, object?> dataSelector)
    {
        if (result.IsSuccess)
        {
            return Ok(new { success = true, data = dataSelector(result.Value!) });
        }

        return BadRequest(new { success = false, error = result.Error?.Message });
    }
}
