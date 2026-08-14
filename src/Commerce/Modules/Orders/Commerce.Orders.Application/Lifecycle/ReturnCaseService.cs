using Commerce.Framework.Contracts.Tenancy;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Inventory.Contracts.Inventory;
using Commerce.Orders.Application.Abstractions;
using Commerce.Orders.Application.Orders;
using Commerce.Orders.Contracts.Orders;
using Commerce.Orders.Domain.Entities;
using Commerce.Orders.Domain.Enums;
using Commerce.Payments.Contracts.Payments;
using Microsoft.Extensions.Logging;

namespace Commerce.Orders.Application.Lifecycle;

public sealed class ReturnCaseService(
    IOrderRepository orderRepository,
    IReturnCaseRepository returnCaseRepository,
    IInventoryOrderService inventoryOrderService,
    IPaymentService paymentService,
    IStoreContext storeContext,
    IEnumerable<IOrderReturnHandler> returnHandlers,
    ILogger<ReturnCaseService> logger) : IReturnAdminService
{
    public async Task<Result<IReadOnlyList<ReturnCaseSummaryDto>>> ListByOrderAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await GetAccessibleOrderAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return Result.Failure<IReadOnlyList<ReturnCaseSummaryDto>>(OrderErrors.OrderNotFound(orderId));
        }

        var cases = await returnCaseRepository.ListByOrderAsync(orderId, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<ReturnCaseSummaryDto>>(cases.Select(ReturnCaseMapper.ToSummary).ToList());
    }

    public async Task<Result<ReturnCaseDetailDto>> GetAsync(int returnCaseId, CancellationToken cancellationToken = default)
    {
        var returnCase = await returnCaseRepository.GetByIdWithItemsAsync(returnCaseId, cancellationToken).ConfigureAwait(false);
        if (returnCase is null || !IsStoreAccessible(returnCase.StoreId))
        {
            return Result.Failure<ReturnCaseDetailDto>(Error.NotFound($"Return case '{returnCaseId}' was not found."));
        }

        return Result.Success(ReturnCaseMapper.ToDetail(returnCase));
    }

    public async Task<Result<ReturnCaseDetailDto>> CreateAsync(
        int orderId,
        CreateReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var order = await GetAccessibleOrderAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return Result.Failure<ReturnCaseDetailDto>(OrderErrors.OrderNotFound(orderId));
        }

        if (order.Status is OrderStatus.Cancelled)
        {
            return Result.Failure<ReturnCaseDetailDto>(OrderErrors.InvalidOrderState("Cancelled orders cannot accept returns."));
        }

        var lineValidation = ValidateReturnLines(order, request.Lines);
        if (lineValidation.IsFailure)
        {
            return Result.Failure<ReturnCaseDetailDto>(lineValidation.Error!);
        }

        var items = request.Lines.Select(line =>
        {
            var orderItem = order.Items.First(x => x.Id == line.OrderItemId);
            return ReturnCaseItem.Create(
                line.OrderItemId,
                orderItem.OfferId,
                orderItem.ProductId,
                line.Quantity);
        });

        ReturnCase returnCase;
        try
        {
            returnCase = ReturnCase.Create(
                order.Id,
                order.StoreId,
                order.CustomerId,
                request.ResolutionType,
                request.Reason,
                order.CurrencyCode,
                request.CustomerNotes,
                items);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure<ReturnCaseDetailDto>(OrderErrors.InvalidOrderState(ex.Message));
        }

        order.AddReturnHistory(string.Empty, ReturnStatus.Requested.ToString(), request.Reason, "admin");
        await returnCaseRepository.AddAsync(returnCase, cancellationToken).ConfigureAwait(false);
        await orderRepository.SaveAsync(order, cancellationToken).ConfigureAwait(false);

        foreach (var handler in returnHandlers)
        {
            await handler.HandleReturnRequestedAsync(returnCase.Id, order.Id, cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("Return case {ReturnCaseId} created for order {OrderId}.", returnCase.Id, order.Id);
        return Result.Success(ReturnCaseMapper.ToDetail(returnCase));
    }

    public async Task<Result<ReturnCaseDetailDto>> ApproveAsync(
        int returnCaseId,
        ApproveReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        var returnCase = await GetAccessibleReturnCaseAsync(returnCaseId, cancellationToken).ConfigureAwait(false);
        if (returnCase is null)
        {
            return Result.Failure<ReturnCaseDetailDto>(Error.NotFound($"Return case '{returnCaseId}' was not found."));
        }

        try
        {
            returnCase.Approve(request.AdminNotes);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<ReturnCaseDetailDto>(OrderErrors.InvalidOrderState(ex.Message));
        }

        await returnCaseRepository.SaveAsync(returnCase, cancellationToken).ConfigureAwait(false);

        foreach (var handler in returnHandlers)
        {
            await handler.HandleReturnApprovedAsync(returnCase.Id, returnCase.OrderId, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success(ReturnCaseMapper.ToDetail(returnCase));
    }

    public async Task<Result<ReturnCaseDetailDto>> RejectAsync(
        int returnCaseId,
        RejectReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        var returnCase = await GetAccessibleReturnCaseAsync(returnCaseId, cancellationToken).ConfigureAwait(false);
        if (returnCase is null)
        {
            return Result.Failure<ReturnCaseDetailDto>(Error.NotFound($"Return case '{returnCaseId}' was not found."));
        }

        try
        {
            returnCase.Reject(request.Reason);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<ReturnCaseDetailDto>(OrderErrors.InvalidOrderState(ex.Message));
        }

        await returnCaseRepository.SaveAsync(returnCase, cancellationToken).ConfigureAwait(false);

        foreach (var handler in returnHandlers)
        {
            await handler.HandleReturnRejectedAsync(returnCase.Id, returnCase.OrderId, request.Reason, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success(ReturnCaseMapper.ToDetail(returnCase));
    }

    public async Task<Result<ReturnCaseDetailDto>> SetReturnShipmentAsync(
        int returnCaseId,
        ReturnShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var returnCase = await GetAccessibleReturnCaseAsync(returnCaseId, cancellationToken).ConfigureAwait(false);
        if (returnCase is null)
        {
            return Result.Failure<ReturnCaseDetailDto>(Error.NotFound($"Return case '{returnCaseId}' was not found."));
        }

        try
        {
            returnCase.SetReturnShipment(request.TrackingNumber);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<ReturnCaseDetailDto>(OrderErrors.InvalidOrderState(ex.Message));
        }

        await returnCaseRepository.SaveAsync(returnCase, cancellationToken).ConfigureAwait(false);
        return Result.Success(ReturnCaseMapper.ToDetail(returnCase));
    }

    public async Task<Result<ReturnCaseDetailDto>> MarkReceivedAsync(
        int returnCaseId,
        CancellationToken cancellationToken = default)
    {
        var returnCase = await GetAccessibleReturnCaseAsync(returnCaseId, cancellationToken).ConfigureAwait(false);
        if (returnCase is null)
        {
            return Result.Failure<ReturnCaseDetailDto>(Error.NotFound($"Return case '{returnCaseId}' was not found."));
        }

        try
        {
            returnCase.MarkReceived();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<ReturnCaseDetailDto>(OrderErrors.InvalidOrderState(ex.Message));
        }

        await returnCaseRepository.SaveAsync(returnCase, cancellationToken).ConfigureAwait(false);
        return Result.Success(ReturnCaseMapper.ToDetail(returnCase));
    }

    public async Task<Result<ReturnCaseDetailDto>> CompleteAsync(
        int returnCaseId,
        CompleteReturnRequest request,
        string? refundIdempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var returnCase = await GetAccessibleReturnCaseAsync(returnCaseId, cancellationToken).ConfigureAwait(false);
        if (returnCase is null)
        {
            return Result.Failure<ReturnCaseDetailDto>(Error.NotFound($"Return case '{returnCaseId}' was not found."));
        }

        var order = await orderRepository.GetByIdWithDetailsAsync(returnCase.OrderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return Result.Failure<ReturnCaseDetailDto>(OrderErrors.OrderNotFound(returnCase.OrderId));
        }

        if (returnCase.Status == ReturnStatus.Received)
        {
            var inventoryLines = returnCase.Items
                .Select(x => new InventoryOrderLineAdjustment(x.OfferId, x.Quantity))
                .ToList();

            await inventoryOrderService
                .RestockForOrderAsync(order.Id, order.StoreId, inventoryLines, "Return received.", cancellationToken)
                .ConfigureAwait(false);

            returnCase.MarkRestocked();
        }

        if (returnCase.ResolutionType == ReturnResolutionType.Refund &&
            returnCase.Status is ReturnStatus.Restocked or ReturnStatus.Received)
        {
            if (string.IsNullOrWhiteSpace(refundIdempotencyKey))
            {
                return Result.Failure<ReturnCaseDetailDto>(OrderErrors.IdempotencyKeyRequired());
            }

            decimal refundAmount = 0m;
            foreach (var item in returnCase.Items)
            {
                var orderItem = order.Items.First(x => x.Id == item.OrderItemId);
                var lineAmount = orderItem.CalculateLineRefundAmount(item.Quantity);
                item.SetRefundAmount(lineAmount);
                refundAmount += lineAmount;
            }

            var paymentResult = await paymentService.GetByOrderIdAsync(order.Id, cancellationToken).ConfigureAwait(false);
            if (paymentResult.IsFailure)
            {
                return Result.Failure<ReturnCaseDetailDto>(paymentResult.Error!);
            }

            var paymentInfo = paymentResult.Value!.Payment;
            var remaining = paymentInfo.Amount - paymentInfo.RefundedAmount;
            if (refundAmount > remaining)
            {
                return Result.Failure<ReturnCaseDetailDto>(
                    OrderErrors.InvalidOrderState("Return refund exceeds remaining payment balance."));
            }

            var refundResult = refundAmount >= remaining
                ? await paymentService.RefundAsync(paymentInfo.Id, "Return refund.", refundIdempotencyKey, cancellationToken).ConfigureAwait(false)
                : await paymentService.PartialRefundAsync(paymentInfo.Id, refundAmount, "Return refund.", refundIdempotencyKey, cancellationToken).ConfigureAwait(false);

            if (refundResult.IsFailure)
            {
                return Result.Failure<ReturnCaseDetailDto>(refundResult.Error!);
            }

            var latestRefund = refundResult.Value!.Refunds.OrderByDescending(x => x.Id).FirstOrDefault();
            returnCase.RecordRefund(refundAmount, latestRefund?.Id ?? 0);
            order.RecordReturn(returnCase.Items.Select(x => (x.OrderItemId, x.Quantity)).ToList());
            await orderRepository.SaveAsync(order, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            returnCase.Complete(request.ReplacementOrderId);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<ReturnCaseDetailDto>(OrderErrors.InvalidOrderState(ex.Message));
        }

        await returnCaseRepository.SaveAsync(returnCase, cancellationToken).ConfigureAwait(false);

        foreach (var handler in returnHandlers)
        {
            await handler.HandleReturnCompletedAsync(returnCase.Id, returnCase.OrderId, cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("Return case {ReturnCaseId} completed.", returnCase.Id);
        return Result.Success(ReturnCaseMapper.ToDetail(returnCase));
    }

    private async Task<Domain.Entities.Order?> GetAccessibleOrderAsync(int orderId, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return null;
        }

        return IsStoreAccessible(order.StoreId) ? order : null;
    }

    private async Task<ReturnCase?> GetAccessibleReturnCaseAsync(int returnCaseId, CancellationToken cancellationToken)
    {
        var returnCase = await returnCaseRepository.GetByIdWithItemsAsync(returnCaseId, cancellationToken).ConfigureAwait(false);
        return returnCase is not null && IsStoreAccessible(returnCase.StoreId) ? returnCase : null;
    }

    private bool IsStoreAccessible(int storeId)
    {
        var currentStoreId = storeContext.CurrentStoreId;
        return currentStoreId.HasValue && currentStoreId.Value == storeId;
    }

    private static Result ValidateReturnLines(Domain.Entities.Order order, IReadOnlyList<OrderLineQuantityRequest> lines)
    {
        if (lines.Count == 0)
        {
            return Result.Failure(OrderErrors.InvalidOrderState("At least one return line is required."));
        }

        foreach (var line in lines)
        {
            var item = order.Items.FirstOrDefault(x => x.Id == line.OrderItemId);
            if (item is null)
            {
                return Result.Failure(OrderErrors.InvalidOrderState($"Order item '{line.OrderItemId}' was not found."));
            }

            if (line.Quantity <= 0 || line.Quantity > item.ReturnableQuantity)
            {
                return Result.Failure(OrderErrors.InvalidOrderState(
                    $"Invalid return quantity for order item '{line.OrderItemId}'."));
            }
        }

        return Result.Success();
    }
}

internal static class ReturnCaseMapper
{
    public static ReturnCaseSummaryDto ToSummary(ReturnCase returnCase) =>
        new(
            returnCase.Id,
            returnCase.OrderId,
            returnCase.Status,
            returnCase.ResolutionType,
            returnCase.Reason,
            returnCase.RefundAmount,
            returnCase.CurrencyCode,
            returnCase.CreatedAtUtc);

    public static ReturnCaseDetailDto ToDetail(ReturnCase returnCase) =>
        new(
            returnCase.Id,
            returnCase.OrderId,
            returnCase.StoreId,
            returnCase.CustomerId,
            returnCase.Status,
            returnCase.ResolutionType,
            returnCase.Reason,
            returnCase.CustomerNotes,
            returnCase.AdminNotes,
            returnCase.ReturnTrackingNumber,
            returnCase.RefundAmount,
            returnCase.CurrencyCode,
            returnCase.RefundId,
            returnCase.ReplacementOrderId,
            returnCase.Items.Select(x => new ReturnCaseItemDto(
                x.Id,
                x.OrderItemId,
                x.OfferId,
                x.ProductId,
                x.Quantity,
                x.RefundAmount)).ToList(),
            returnCase.CreatedAtUtc,
            returnCase.UpdatedAtUtc);
}
