using Commerce.Audit.Contracts;
using Commerce.Audit.Infrastructure.Security;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Host.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Audit;

[ApiController]
[Route("api/admin/audit")]
public sealed class AdminAuditController(IAuditQueryService auditQueryService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(AuditPermissions.View)]
    public async Task<IActionResult> List([FromQuery] AuditQuery query, CancellationToken cancellationToken)
    {
        var result = await auditQueryService.ListAsync(query, cancellationToken).ConfigureAwait(false);
        return AuditActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("verify-chain")]
    [RequirePermission(AuditPermissions.VerifyChain)]
    public async Task<IActionResult> VerifyChain([FromQuery] int? storeId, CancellationToken cancellationToken)
    {
        var result = await auditQueryService.VerifyChainAsync(storeId, cancellationToken).ConfigureAwait(false);
        return AuditActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("retention/apply")]
    [RequirePermission(AuditPermissions.ManageRetention)]
    public async Task<IActionResult> ApplyRetention(CancellationToken cancellationToken)
    {
        var result = await auditQueryService.ApplyRetentionPolicyAsync(cancellationToken).ConfigureAwait(false);
        return AuditActionResults.ToActionResult(this, result, value => value);
    }
}

internal static class AuditActionResults
{
    internal static IActionResult ToActionResult<T>(
        ControllerBase controller,
        Result<T> result,
        Func<T, object?> dataSelector,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return controller.StatusCode(successStatusCode, new { success = true, data = dataSelector(result.Value!) });
        }

        return MapFailure(controller, result.Error!);
    }

    private static IActionResult MapFailure(ControllerBase controller, Error error) =>
        error.Type switch
        {
            ErrorType.NotFound => controller.NotFound(new { success = false, error = error.Message }),
            ErrorType.Conflict => controller.Conflict(new { success = false, error = error.Message }),
            _ => controller.BadRequest(new { success = false, error = error.Message })
        };
}
