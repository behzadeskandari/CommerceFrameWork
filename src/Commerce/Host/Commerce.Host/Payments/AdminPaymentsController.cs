using Commerce.Host.Authorization;
using Commerce.Host.Payments;
using Commerce.Payments.Contracts.Admin;
using Commerce.Payments.Contracts.Payments;
using Commerce.Payments.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Payments;

[ApiController]
[Route("api/admin/payments")]
public sealed class AdminPaymentsController(IPaymentAdminService paymentAdminService, IPaymentService paymentService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PaymentsPermissions.View)]
    public async Task<IActionResult> List([FromQuery] PaymentListQuery query, CancellationToken cancellationToken)
    {
        var result = await paymentAdminService.ListPaymentsAsync(query, cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(PaymentsPermissions.View)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await paymentAdminService.GetPaymentAsync(id, cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{id:int}/transactions")]
    [RequirePermission(PaymentsPermissions.View)]
    public async Task<IActionResult> GetTransactions(int id, CancellationToken cancellationToken)
    {
        var result = await paymentAdminService.GetTransactionsAsync(id, cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("{id:int}/capture")]
    [RequirePermission(PaymentsPermissions.Manage)]
    public async Task<IActionResult> Capture(int id, CancellationToken cancellationToken)
    {
        var result = await paymentService.CaptureAsync(id, cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("{id:int}/void")]
    [RequirePermission(PaymentsPermissions.Manage)]
    public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
    {
        var result = await paymentService.VoidAsync(id, cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("{id:int}/refund")]
    [RequirePermission(PaymentsPermissions.Refund)]
    public async Task<IActionResult> Refund(int id, [FromBody] RefundPaymentRequest? request, CancellationToken cancellationToken)
    {
        var result = await paymentService.RefundAsync(id, request?.Reason, cancellationToken: cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("{id:int}/partial-refund")]
    [RequirePermission(PaymentsPermissions.Refund)]
    public async Task<IActionResult> PartialRefund(int id, [FromBody] PartialRefundPaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await paymentService
            .PartialRefundAsync(id, request.Amount, request.Reason, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return PaymentActionResults.ToActionResult(this, result, value => value);
    }
}

public sealed record RefundPaymentRequest(string? Reason);

public sealed record PartialRefundPaymentRequest(decimal Amount, string? Reason);

[ApiController]
[Route("api/admin/payment-methods")]
public sealed class AdminPaymentMethodsController(IPaymentAdminService paymentAdminService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PaymentsPermissions.View)]
    public async Task<IActionResult> List([FromQuery] int? storeId, CancellationToken cancellationToken)
    {
        var result = await paymentAdminService.ListMethodsAsync(storeId, cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(PaymentsPermissions.View)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await paymentAdminService.GetMethodAsync(id, cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost]
    [RequirePermission(PaymentsPermissions.Configure)]
    public async Task<IActionResult> Create([FromBody] CreatePaymentMethodRequest request, CancellationToken cancellationToken)
    {
        var result = await paymentAdminService.CreateMethodAsync(request, cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result, value => value, StatusCodes.Status201Created);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(PaymentsPermissions.Configure)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePaymentMethodRequest request, CancellationToken cancellationToken)
    {
        var result = await paymentAdminService.UpdateMethodAsync(id, request, cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result, value => value);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PaymentsPermissions.Configure)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await paymentAdminService.DeleteMethodAsync(id, cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result);
    }
}
