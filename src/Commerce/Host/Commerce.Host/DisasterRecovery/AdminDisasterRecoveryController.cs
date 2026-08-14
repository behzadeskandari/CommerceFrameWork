using Commerce.DisasterRecovery.Contracts;
using Commerce.DisasterRecovery.Infrastructure.Security;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Host.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.DisasterRecovery;

[ApiController]
[Route("api/admin/disaster-recovery")]
public sealed class AdminDisasterRecoveryController(
    IBackupService backupService,
    IBackupVerificationService verificationService,
    IRecoveryTestService recoveryTestService,
    IDataIntegrityService integrityService,
    IDisasterRecoveryMetadataService metadataService) : ControllerBase
{
    [HttpGet("targets")]
    [RequirePermission(DisasterRecoveryPermissions.View)]
    public IActionResult GetTargets() =>
        Ok(new { success = true, data = metadataService.GetTargets() });

    [HttpGet("backups")]
    [RequirePermission(DisasterRecoveryPermissions.View)]
    public async Task<IActionResult> ListBackups(CancellationToken cancellationToken)
    {
        var result = await backupService.ListBackupsAsync(cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
    }

    [HttpGet("backups/{id:long}")]
    [RequirePermission(DisasterRecoveryPermissions.View)]
    public async Task<IActionResult> GetBackup(long id, CancellationToken cancellationToken)
    {
        var result = await backupService.GetBackupAsync(id, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
    }

    [HttpPost("backups/create")]
    [RequirePermission(DisasterRecoveryPermissions.CreateBackup)]
    public async Task<IActionResult> CreateBackup(CancellationToken cancellationToken)
    {
        var result = await backupService.CreateBackupAsync(cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value, StatusCodes.Status201Created);
    }

    [HttpPost("backups/{id:long}/verify")]
    [RequirePermission(DisasterRecoveryPermissions.VerifyBackup)]
    public async Task<IActionResult> VerifyBackup(long id, CancellationToken cancellationToken)
    {
        var result = await verificationService.VerifyChecksumsAsync(id, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
    }

    [HttpPost("backups/{id:long}/recovery-test")]
    [RequirePermission(DisasterRecoveryPermissions.RunRecoveryTest)]
    public async Task<IActionResult> RunRecoveryTest(long id, CancellationToken cancellationToken)
    {
        var result = await recoveryTestService.RunRecoveryTestAsync(id, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
    }

    [HttpPost("retention/apply")]
    [RequirePermission(DisasterRecoveryPermissions.ManageRetention)]
    public async Task<IActionResult> ApplyRetention(CancellationToken cancellationToken)
    {
        var result = await backupService.ApplyRetentionPolicyAsync(cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, () => new { applied = true });
    }

    [HttpGet("integrity")]
    [RequirePermission(DisasterRecoveryPermissions.View)]
    public async Task<IActionResult> GetIntegrity(CancellationToken cancellationToken)
    {
        var result = await integrityService.GetLiveIntegrityReportAsync(cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
    }

    private IActionResult ToActionResult<T>(Result<T> result, Func<T, object?> selector, int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return StatusCode(successStatusCode, new { success = true, data = selector(result.Value!) });
        }

        return MapFailure(result.Error!);
    }

    private IActionResult ToActionResult(Result result, Func<object?> selector, int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return StatusCode(successStatusCode, new { success = true, data = selector() });
        }

        return MapFailure(result.Error!);
    }

    private IActionResult MapFailure(Error error) =>
        error.Type switch
        {
            ErrorType.NotFound => NotFound(new { success = false, error = error.Message }),
            ErrorType.Conflict => Conflict(new { success = false, error = error.Message }),
            _ => BadRequest(new { success = false, error = error.Message })
        };
}
