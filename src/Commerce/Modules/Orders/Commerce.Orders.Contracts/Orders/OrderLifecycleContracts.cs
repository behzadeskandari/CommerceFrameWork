using Commerce.Framework.Core.Results;
using Commerce.Orders.Domain.Enums;

namespace Commerce.Orders.Contracts.Orders;

public sealed record OrderLineQuantityRequest(int OrderItemId, int Quantity);

public sealed record PartialCancelOrderRequest(
    IReadOnlyList<OrderLineQuantityRequest> Lines,
    string? Reason);

public sealed record RefundOrderRequest(
    IReadOnlyList<OrderLineQuantityRequest>? Lines,
    string? Reason);

public sealed record ConfirmOrderRequest(string? Reason);

public sealed record CompleteOrderRequest(string? Reason);

public sealed record CreateReturnRequest(
    ReturnResolutionType ResolutionType,
    string Reason,
    string? CustomerNotes,
    IReadOnlyList<OrderLineQuantityRequest> Lines);

public sealed record ApproveReturnRequest(string? AdminNotes);

public sealed record RejectReturnRequest(string Reason);

public sealed record ReturnShipmentRequest(string? TrackingNumber);

public sealed record CompleteReturnRequest(int? ReplacementOrderId);

public sealed record ReturnCaseItemDto(
    int Id,
    int OrderItemId,
    int OfferId,
    int ProductId,
    int Quantity,
    decimal RefundAmount);

public sealed record ReturnCaseSummaryDto(
    int Id,
    int OrderId,
    ReturnStatus Status,
    ReturnResolutionType ResolutionType,
    string Reason,
    decimal RefundAmount,
    string CurrencyCode,
    DateTime CreatedAtUtc);

public sealed record ReturnCaseDetailDto(
    int Id,
    int OrderId,
    int StoreId,
    int? CustomerId,
    ReturnStatus Status,
    ReturnResolutionType ResolutionType,
    string Reason,
    string? CustomerNotes,
    string? AdminNotes,
    string? ReturnTrackingNumber,
    decimal RefundAmount,
    string CurrencyCode,
    int? RefundId,
    int? ReplacementOrderId,
    IReadOnlyList<ReturnCaseItemDto> Items,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record RefundOrderResultDto(
    int OrderId,
    int PaymentId,
    int? RefundId,
    decimal RefundAmount,
    string CurrencyCode,
    bool IsFullRefund);

public interface IOrderLifecycleService
{
    Task<Result<OrderDetailDto>> ConfirmAsync(
        int orderId,
        ConfirmOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<OrderDetailDto>> MarkProcessingAsync(
        int orderId,
        CancellationToken cancellationToken = default);

    Task<Result<OrderDetailDto>> CompleteAsync(
        int orderId,
        CompleteOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<OrderDetailDto>> CancelPartialAsync(
        int orderId,
        PartialCancelOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<RefundOrderResultDto>> RefundAsync(
        int orderId,
        RefundOrderRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default);
}

public interface IReturnAdminService
{
    Task<Result<IReadOnlyList<ReturnCaseSummaryDto>>> ListByOrderAsync(
        int orderId,
        CancellationToken cancellationToken = default);

    Task<Result<ReturnCaseDetailDto>> GetAsync(int returnCaseId, CancellationToken cancellationToken = default);

    Task<Result<ReturnCaseDetailDto>> CreateAsync(
        int orderId,
        CreateReturnRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ReturnCaseDetailDto>> ApproveAsync(
        int returnCaseId,
        ApproveReturnRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ReturnCaseDetailDto>> RejectAsync(
        int returnCaseId,
        RejectReturnRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ReturnCaseDetailDto>> SetReturnShipmentAsync(
        int returnCaseId,
        ReturnShipmentRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ReturnCaseDetailDto>> MarkReceivedAsync(
        int returnCaseId,
        CancellationToken cancellationToken = default);

    Task<Result<ReturnCaseDetailDto>> CompleteAsync(
        int returnCaseId,
        CompleteReturnRequest request,
        string? refundIdempotencyKey,
        CancellationToken cancellationToken = default);
}
