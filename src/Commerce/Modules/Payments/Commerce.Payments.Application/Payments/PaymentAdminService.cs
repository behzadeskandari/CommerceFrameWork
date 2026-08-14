using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Payments.Application.Abstractions;
using Commerce.Payments.Contracts.Admin;
using Commerce.Payments.Contracts.Payments;
using Commerce.Payments.Domain.Entities;

namespace Commerce.Payments.Application.Payments;

public sealed class PaymentAdminService(IPaymentRepository repository, IPaymentService paymentService) : IPaymentAdminService
{
    public async Task<Result<PagedPaymentSummaryResult>> ListPaymentsAsync(
        PaymentListQuery query,
        CancellationToken cancellationToken = default)
    {
        var criteria = new PaymentListCriteria(
            Math.Max(1, query.Page),
            Math.Clamp(query.PageSize, 1, 100),
            query.StoreId,
            query.OrderId,
            query.Status,
            query.CreatedFromUtc,
            query.CreatedToUtc);

        var (items, total) = await repository.ListAsync(criteria, cancellationToken).ConfigureAwait(false);
        return Result.Success(new PagedPaymentSummaryResult(
            items.Select(x => new PaymentSummaryDto(
                x.Id,
                x.StoreId,
                x.OrderId,
                x.Currency,
                x.Amount,
                x.Status,
                x.ProviderSystemName,
                x.CreatedAtUtc)).ToList(),
            criteria.Page,
            criteria.PageSize,
            total));
    }

    public Task<Result<PaymentDetailDto>> GetPaymentAsync(int paymentId, CancellationToken cancellationToken = default) =>
        paymentService.GetByIdAsync(paymentId, cancellationToken);

    public async Task<Result<IReadOnlyList<PaymentTransactionDto>>> GetTransactionsAsync(
        int paymentId,
        CancellationToken cancellationToken = default)
    {
        var payment = await repository.GetByIdWithDetailsAsync(paymentId, cancellationToken).ConfigureAwait(false);
        if (payment is null)
        {
            return Result.Failure<IReadOnlyList<PaymentTransactionDto>>(PaymentErrors.PaymentNotFound(paymentId));
        }

        return Result.Success<IReadOnlyList<PaymentTransactionDto>>(
            payment.Transactions.Select(PaymentMapper.ToTransactionDto).ToList());
    }

    public async Task<Result<IReadOnlyList<PaymentMethodSummaryDto>>> ListMethodsAsync(
        int? storeId,
        CancellationToken cancellationToken = default)
    {
        var methods = await repository.ListMethodsAsync(storeId, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<PaymentMethodSummaryDto>>(
            methods.Where(x => !x.IsDeleted).Select(MapMethodSummary).ToList());
    }

    public async Task<Result<PaymentMethodDetailDto>> GetMethodAsync(int id, CancellationToken cancellationToken = default)
    {
        var method = await repository.GetMethodByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return method is null || method.IsDeleted
            ? Result.Failure<PaymentMethodDetailDto>(Error.NotFound($"Payment method '{id}' was not found."))
            : Result.Success(MapMethodDetail(method));
    }

    public async Task<Result<PaymentMethodDetailDto>> CreateMethodAsync(
        CreatePaymentMethodRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var method = PaymentMethod.Create(
            request.StoreId,
            request.Name,
            request.SystemName,
            request.ProviderSystemName,
            request.DisplayName,
            request.IsActive,
            request.DisplayOrder,
            request.RequiresRedirect,
            request.SupportsGuest,
            request.SupportsFreeOrders,
            request.ConfigurationJson);

        await repository.AddMethodAsync(method, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapMethodDetail(method));
    }

    public async Task<Result<PaymentMethodDetailDto>> UpdateMethodAsync(
        int id,
        UpdatePaymentMethodRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var method = await repository.GetMethodByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (method is null || method.IsDeleted)
        {
            return Result.Failure<PaymentMethodDetailDto>(Error.NotFound($"Payment method '{id}' was not found."));
        }

        method.Update(
            request.Name,
            request.DisplayName,
            request.IsActive,
            request.DisplayOrder,
            request.RequiresRedirect,
            request.SupportsGuest,
            request.SupportsFreeOrders,
            request.ConfigurationJson);

        await repository.SaveMethodAsync(method, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapMethodDetail(method));
    }

    public async Task<Result> DeleteMethodAsync(int id, CancellationToken cancellationToken = default)
    {
        var method = await repository.GetMethodByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (method is null || method.IsDeleted)
        {
            return Result.Failure(Error.NotFound($"Payment method '{id}' was not found."));
        }

        method.SoftDelete();
        await repository.SaveMethodAsync(method, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private static PaymentMethodSummaryDto MapMethodSummary(PaymentMethod method) =>
        new(
            method.Id,
            method.StoreId,
            method.Name,
            method.SystemName,
            method.ProviderSystemName,
            method.DisplayName,
            method.IsActive,
            method.DisplayOrder,
            method.RequiresRedirect,
            method.SupportsGuest,
            method.SupportsFreeOrders);

    private static PaymentMethodDetailDto MapMethodDetail(PaymentMethod method) =>
        new(
            method.Id,
            method.StoreId,
            method.Name,
            method.SystemName,
            method.ProviderSystemName,
            method.DisplayName,
            method.IsActive,
            method.DisplayOrder,
            method.RequiresRedirect,
            method.SupportsGuest,
            method.SupportsFreeOrders,
            method.ConfigurationJson,
            method.CreatedAtUtc,
            method.UpdatedAtUtc);
}
