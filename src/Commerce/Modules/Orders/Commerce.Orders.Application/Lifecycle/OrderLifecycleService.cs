using Commerce.Framework.Contracts.Tenancy;
using Commerce.Framework.Core.Results;
using Commerce.Inventory.Contracts.Inventory;
using Commerce.Orders.Application.Abstractions;
using Commerce.Orders.Application.Orders;
using Commerce.Orders.Contracts.Orders;
using Commerce.Orders.Domain.Enums;
using Commerce.Payments.Contracts.Payments;
using Commerce.Shipping.Contracts.Shipments;
using Microsoft.Extensions.Logging;
using OrderPaymentStatus = Commerce.Orders.Domain.Enums.PaymentStatus;

namespace Commerce.Orders.Application.Lifecycle;

public sealed class OrderLifecycleService(
    IOrderRepository orderRepository,
    IInventoryOrderService inventoryOrderService,
    IPaymentService paymentService,
    IShipmentAdminService shipmentAdminService,
    IStoreContext storeContext,
    ILogger<OrderLifecycleService> logger) : IOrderLifecycleService
{
    public async Task<Result<OrderDetailDto>> ConfirmAsync(
        int orderId,
        ConfirmOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await GetAccessibleOrderAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return Result.Failure<OrderDetailDto>(OrderErrors.OrderNotFound(orderId));
        }

        try
        {
            order.Confirm(request.Reason);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<OrderDetailDto>(OrderErrors.InvalidOrderState(ex.Message));
        }

        await orderRepository.SaveAsync(order, cancellationToken).ConfigureAwait(false);
        return Result.Success(OrderMapper.ToDetail(order));
    }

    public async Task<Result<OrderDetailDto>> MarkProcessingAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await GetAccessibleOrderAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return Result.Failure<OrderDetailDto>(OrderErrors.OrderNotFound(orderId));
        }

        try
        {
            order.MarkProcessing();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<OrderDetailDto>(OrderErrors.InvalidOrderState(ex.Message));
        }

        await orderRepository.SaveAsync(order, cancellationToken).ConfigureAwait(false);
        return Result.Success(OrderMapper.ToDetail(order));
    }

    public async Task<Result<OrderDetailDto>> CompleteAsync(
        int orderId,
        CompleteOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await GetAccessibleOrderAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return Result.Failure<OrderDetailDto>(OrderErrors.OrderNotFound(orderId));
        }

        try
        {
            order.Complete(request.Reason);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<OrderDetailDto>(OrderErrors.InvalidOrderState(ex.Message));
        }

        await orderRepository.SaveAsync(order, cancellationToken).ConfigureAwait(false);
        return Result.Success(OrderMapper.ToDetail(order));
    }

    public async Task<Result<OrderDetailDto>> CancelPartialAsync(
        int orderId,
        PartialCancelOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var order = await GetAccessibleOrderAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return Result.Failure<OrderDetailDto>(OrderErrors.OrderNotFound(orderId));
        }

        var lineValidation = ValidateLines(order, request.Lines, useReturnableQuantity: false);
        if (lineValidation.IsFailure)
        {
            return Result.Failure<OrderDetailDto>(lineValidation.Error!);
        }

        var reason = string.IsNullOrWhiteSpace(request.Reason)
            ? "Partial cancellation."
            : request.Reason.Trim();

        var inventoryLines = BuildInventoryLines(order, request.Lines);
        var isPaid = order.PaymentStatus is OrderPaymentStatus.Paid or OrderPaymentStatus.PartiallyRefunded;

        if (isPaid)
        {
            await inventoryOrderService
                .RestockForOrderAsync(order.Id, order.StoreId, inventoryLines, reason, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            await inventoryOrderService
                .ReleasePartialForOrderAsync(order.Id, order.StoreId, inventoryLines, cancellationToken)
                .ConfigureAwait(false);
        }

        try
        {
            order.CancelPartial(
                request.Lines.Select(x => (x.OrderItemId, x.Quantity)).ToList(),
                reason,
                "admin");
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<OrderDetailDto>(OrderErrors.InvalidOrderState(ex.Message));
        }

        await orderRepository.SaveAsync(order, cancellationToken).ConfigureAwait(false);

        if (order.Status == OrderStatus.Cancelled)
        {
            await shipmentAdminService
                .CancelOpenShipmentsForOrderAsync(order.Id, reason, cancellationToken)
                .ConfigureAwait(false);
        }

        logger.LogInformation("Order {OrderNumber} partially cancelled.", order.OrderNumber);
        return Result.Success(OrderMapper.ToDetail(order));
    }

    public async Task<Result<RefundOrderResultDto>> RefundAsync(
        int orderId,
        RefundOrderRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Result.Failure<RefundOrderResultDto>(OrderErrors.IdempotencyKeyRequired());
        }

        var order = await GetAccessibleOrderAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return Result.Failure<RefundOrderResultDto>(OrderErrors.OrderNotFound(orderId));
        }

        if (order.PaymentStatus is not OrderPaymentStatus.Paid and not OrderPaymentStatus.PartiallyRefunded)
        {
            return Result.Failure<RefundOrderResultDto>(
                OrderErrors.InvalidOrderState("Order payment is not refundable."));
        }

        var paymentResult = await paymentService.GetByOrderIdAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (paymentResult.IsFailure)
        {
            return Result.Failure<RefundOrderResultDto>(paymentResult.Error!);
        }

        var payment = paymentResult.Value!;
        var paymentInfo = payment.Payment;
        decimal refundAmount;
        var isFullRefund = request.Lines is null || request.Lines.Count == 0;

        if (isFullRefund)
        {
            refundAmount = paymentInfo.Amount - paymentInfo.RefundedAmount;
        }
        else
        {
            var lineValidation = ValidateLines(order, request.Lines!, useReturnableQuantity: true);
            if (lineValidation.IsFailure)
            {
                return Result.Failure<RefundOrderResultDto>(lineValidation.Error!);
            }

            refundAmount = order.CalculateRefundAmount(
                request.Lines.Select(x => (x.OrderItemId, x.Quantity)).ToList());
        }

        if (refundAmount <= 0)
        {
            return Result.Failure<RefundOrderResultDto>(
                OrderErrors.InvalidOrderState("Refund amount must be greater than zero."));
        }

        var remaining = paymentInfo.Amount - paymentInfo.RefundedAmount;
        if (refundAmount > remaining)
        {
            return Result.Failure<RefundOrderResultDto>(
                OrderErrors.InvalidOrderState("Calculated refund exceeds remaining payment balance."));
        }

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? "Order refund." : request.Reason.Trim();
        var refundPaymentResult = isFullRefund || refundAmount >= remaining
            ? await paymentService.RefundAsync(paymentInfo.Id, reason, idempotencyKey, cancellationToken).ConfigureAwait(false)
            : await paymentService.PartialRefundAsync(paymentInfo.Id, refundAmount, reason, idempotencyKey, cancellationToken).ConfigureAwait(false);

        if (refundPaymentResult.IsFailure)
        {
            return Result.Failure<RefundOrderResultDto>(refundPaymentResult.Error!);
        }

        var updatedPayment = refundPaymentResult.Value!;
        var latestRefund = updatedPayment.Refunds.OrderByDescending(x => x.Id).FirstOrDefault();

        if (!isFullRefund && request.Lines is not null)
        {
            await inventoryOrderService
                .RestockForOrderAsync(
                    order.Id,
                    order.StoreId,
                    BuildInventoryLines(order, request.Lines),
                    reason,
                    cancellationToken)
                .ConfigureAwait(false);

            order.RecordReturn(request.Lines.Select(x => (x.OrderItemId, x.Quantity)).ToList());
            await orderRepository.SaveAsync(order, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success(new RefundOrderResultDto(
            order.Id,
            paymentInfo.Id,
            latestRefund?.Id,
            refundAmount,
            order.CurrencyCode,
            isFullRefund || refundAmount >= remaining));
    }

    private async Task<Domain.Entities.Order?> GetAccessibleOrderAsync(int orderId, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return null;
        }

        var storeId = storeContext.CurrentStoreId;
        return storeId.HasValue && order.StoreId == storeId.Value ? order : null;
    }

    private static Result ValidateLines(
        Domain.Entities.Order order,
        IReadOnlyList<OrderLineQuantityRequest> lines,
        bool useReturnableQuantity)
    {
        if (lines.Count == 0)
        {
            return Result.Failure(OrderErrors.InvalidOrderState("At least one order line is required."));
        }

        foreach (var line in lines)
        {
            var item = order.Items.FirstOrDefault(x => x.Id == line.OrderItemId);
            if (item is null)
            {
                return Result.Failure(OrderErrors.InvalidOrderState($"Order item '{line.OrderItemId}' was not found."));
            }

            var available = useReturnableQuantity ? item.ReturnableQuantity : item.ActiveQuantity;
            if (line.Quantity <= 0 || line.Quantity > available)
            {
                return Result.Failure(OrderErrors.InvalidOrderState(
                    $"Invalid quantity for order item '{line.OrderItemId}'."));
            }
        }

        return Result.Success();
    }

    private static IReadOnlyList<InventoryOrderLineAdjustment> BuildInventoryLines(
        Domain.Entities.Order order,
        IReadOnlyList<OrderLineQuantityRequest> lines) =>
        lines
            .Select(line =>
            {
                var item = order.Items.First(x => x.Id == line.OrderItemId);
                return new InventoryOrderLineAdjustment(item.OfferId, line.Quantity);
            })
            .ToList();
}
