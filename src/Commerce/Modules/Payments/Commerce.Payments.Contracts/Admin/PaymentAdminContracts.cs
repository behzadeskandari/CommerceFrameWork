using Commerce.Framework.Core.Results;

using Commerce.Payments.Domain.Enums;



namespace Commerce.Payments.Contracts.Admin;



public sealed record PaymentMethodSummaryDto(

    int Id,

    int StoreId,

    string Name,

    string SystemName,

    string ProviderSystemName,

    string DisplayName,

    bool IsActive,

    int DisplayOrder,

    bool RequiresRedirect,

    bool SupportsGuest,

    bool SupportsFreeOrders);



public sealed record PaymentMethodDetailDto(

    int Id,

    int StoreId,

    string Name,

    string SystemName,

    string ProviderSystemName,

    string DisplayName,

    bool IsActive,

    int DisplayOrder,

    bool RequiresRedirect,

    bool SupportsGuest,

    bool SupportsFreeOrders,

    string? ConfigurationJson,

    DateTime CreatedAtUtc,

    DateTime UpdatedAtUtc);



public sealed record CreatePaymentMethodRequest(

    int StoreId,

    string Name,

    string SystemName,

    string ProviderSystemName,

    string DisplayName,

    bool IsActive,

    int DisplayOrder,

    bool RequiresRedirect,

    bool SupportsGuest,

    bool SupportsFreeOrders,

    string? ConfigurationJson = null);



public sealed record UpdatePaymentMethodRequest(

    string Name,

    string DisplayName,

    bool IsActive,

    int DisplayOrder,

    bool RequiresRedirect,

    bool SupportsGuest,

    bool SupportsFreeOrders,

    string? ConfigurationJson = null);



public sealed record PaymentSummaryDto(

    int Id,

    int StoreId,

    int OrderId,

    string Currency,

    decimal Amount,

    PaymentStatus Status,

    string ProviderSystemName,

    DateTime CreatedAtUtc);



public sealed record PagedPaymentSummaryResult(

    IReadOnlyList<PaymentSummaryDto> Items,

    int Page,

    int PageSize,

    int TotalCount);



public sealed record PaymentListQuery(

    int Page = 1,

    int PageSize = 20,

    int? StoreId = null,

    int? OrderId = null,

    PaymentStatus? Status = null,

    DateTime? CreatedFromUtc = null,

    DateTime? CreatedToUtc = null);



public interface IPaymentAdminService

{

    Task<Result<PagedPaymentSummaryResult>> ListPaymentsAsync(

        PaymentListQuery query,

        CancellationToken cancellationToken = default);



    Task<Result<Commerce.Payments.Contracts.Payments.PaymentDetailDto>> GetPaymentAsync(

        int paymentId,

        CancellationToken cancellationToken = default);



    Task<Result<IReadOnlyList<Commerce.Payments.Contracts.Payments.PaymentTransactionDto>>> GetTransactionsAsync(

        int paymentId,

        CancellationToken cancellationToken = default);



    Task<Result<IReadOnlyList<PaymentMethodSummaryDto>>> ListMethodsAsync(

        int? storeId,

        CancellationToken cancellationToken = default);



    Task<Result<PaymentMethodDetailDto>> GetMethodAsync(int id, CancellationToken cancellationToken = default);



    Task<Result<PaymentMethodDetailDto>> CreateMethodAsync(

        CreatePaymentMethodRequest request,

        CancellationToken cancellationToken = default);



    Task<Result<PaymentMethodDetailDto>> UpdateMethodAsync(

        int id,

        UpdatePaymentMethodRequest request,

        CancellationToken cancellationToken = default);



    Task<Result> DeleteMethodAsync(int id, CancellationToken cancellationToken = default);

}

