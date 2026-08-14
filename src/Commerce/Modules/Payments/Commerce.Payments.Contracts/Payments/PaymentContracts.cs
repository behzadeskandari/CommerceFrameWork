using Commerce.Framework.Core.Results;

using Commerce.Payments.Domain.Enums;



namespace Commerce.Payments.Contracts.Payments;



public sealed record PaymentRequest(
    int PaymentId,
    int StoreId,
    int OrderId,
    int? CustomerId,
    string Currency,
    decimal Amount,
    string PaymentMethodSystemName,
    string? ReturnUrl = null,
    string? CancelUrl = null,
    string? IdempotencyKey = null,
    string? ProviderPaymentId = null,
    IReadOnlyDictionary<string, string>? Metadata = null);



public sealed record PaymentResult(

    bool Success,

    PaymentStatus Status,

    string? ProviderPaymentId = null,

    string? RedirectUrl = null,

    string? Instructions = null,

    string? FailureCode = null,

    string? FailureMessage = null);



public sealed record PaymentVerificationResult(

    bool Success,

    PaymentStatus Status,

    string? ProviderPaymentId = null,

    string? FailureCode = null,

    string? FailureMessage = null);



public sealed record RefundRequest(
    int PaymentId,
    int StoreId,
    decimal Amount,
    string Currency,
    string? Reason = null,
    string? IdempotencyKey = null,
    string? ProviderPaymentId = null);



public sealed record RefundResult(

    bool Success,

    RefundStatus Status,

    string? ProviderTransactionId = null,

    string? FailureCode = null,

    string? FailureMessage = null);



public interface IPaymentProvider

{

    string ProviderSystemName { get; }



    Task<PaymentResult> CreatePaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default);



    Task<PaymentVerificationResult> GetPaymentStatusAsync(

        int paymentId,

        string? providerPaymentId,

        CancellationToken cancellationToken = default);



    Task<PaymentVerificationResult> VerifyPaymentAsync(

        int paymentId,

        string? providerPaymentId,

        IReadOnlyDictionary<string, string>? callbackData = null,

        CancellationToken cancellationToken = default);



    Task<PaymentResult> CaptureAsync(PaymentRequest request, CancellationToken cancellationToken = default);



    Task<PaymentResult> VoidAsync(PaymentRequest request, CancellationToken cancellationToken = default);



    Task<RefundResult> RefundAsync(RefundRequest request, CancellationToken cancellationToken = default);

}



public sealed record CreatePaymentForOrderRequest(

    int OrderId,

    string? PaymentMethodId = null,

    string? PaymentMethodSystemName = null,

    string? ReturnUrl = null,

    string? CancelUrl = null);



public sealed record PaymentDto(

    int Id,

    int StoreId,

    int OrderId,

    int? CustomerId,

    string Currency,

    decimal Amount,

    PaymentStatus Status,

    string ProviderSystemName,

    string? ProviderPaymentId,

    decimal RefundedAmount,

    DateTime CreatedAtUtc,

    DateTime UpdatedAtUtc);



public sealed record PaymentDetailDto(

    PaymentDto Payment,

    IReadOnlyList<PaymentTransactionDto> Transactions,

    IReadOnlyList<PaymentAttemptDto> Attempts,

    IReadOnlyList<RefundDto> Refunds);



public sealed record PaymentTransactionDto(

    int Id,

    PaymentTransactionType TransactionType,

    decimal Amount,

    string Currency,

    PaymentTransactionStatus Status,

    string? ProviderTransactionId,

    string? FailureCode,

    string? FailureMessage,

    DateTime CreatedAtUtc);



public sealed record PaymentAttemptDto(

    int Id,

    int AttemptNumber,

    PaymentAttemptStatus Status,

    string? FailureMessage,

    DateTime CreatedAtUtc);



public sealed record RefundDto(

    int Id,

    decimal Amount,

    string Currency,

    RefundStatus Status,

    string? Reason,

    DateTime CreatedAtUtc);



public sealed record CreatePaymentResultDto(

    int PaymentId,

    PaymentStatus Status,

    string? RedirectUrl,

    string? Instructions);



public interface IPaymentService

{

    Task<Result<CreatePaymentResultDto>> CreateForOrderAsync(

        CreatePaymentForOrderRequest request,

        string? idempotencyKey,

        CancellationToken cancellationToken = default);



    Task<Result<PaymentDetailDto>> GetByIdAsync(int paymentId, CancellationToken cancellationToken = default);



    Task<Result<PaymentDetailDto>> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);



    Task<Result<PaymentDetailDto>> ProcessCallbackAsync(

        string providerSystemName,

        string callbackKey,

        string payloadHash,

        IReadOnlyDictionary<string, string> callbackData,

        CancellationToken cancellationToken = default);



    Task<Result<PaymentDetailDto>> CaptureAsync(int paymentId, CancellationToken cancellationToken = default);



    Task<Result<PaymentDetailDto>> VoidAsync(int paymentId, CancellationToken cancellationToken = default);



    Task<Result<PaymentDetailDto>> RefundAsync(
        int paymentId,
        string? reason,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default);



    Task<Result<PaymentDetailDto>> PartialRefundAsync(

        int paymentId,

        decimal amount,

        string? reason,

        string? idempotencyKey = null,

        CancellationToken cancellationToken = default);

}



public interface IOrderPaymentSyncService

{

    Task SyncAuthorizedAsync(int orderId, string? reason = null, CancellationToken cancellationToken = default);



    Task SyncPaidAsync(int orderId, string? reason = null, CancellationToken cancellationToken = default);



    Task SyncFailedAsync(int orderId, string? reason = null, CancellationToken cancellationToken = default);



    Task SyncPartialRefundAsync(int orderId, string? reason = null, CancellationToken cancellationToken = default);



    Task SyncFullRefundAsync(int orderId, string? reason = null, CancellationToken cancellationToken = default);

}

