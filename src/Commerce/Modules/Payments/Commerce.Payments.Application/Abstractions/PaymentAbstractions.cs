using Commerce.Payments.Domain.Entities;

using Commerce.Payments.Domain.Enums;



namespace Commerce.Payments.Application.Abstractions;



public interface IPaymentRepository

{

    Task<Payment?> GetByIdWithDetailsAsync(int paymentId, CancellationToken cancellationToken = default);



    Task<Payment?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);



    Task<Payment?> GetByIdempotencyKeyAsync(int storeId, string idempotencyKey, CancellationToken cancellationToken = default);



    Task<(IReadOnlyList<Payment> Items, int TotalCount)> ListAsync(

        PaymentListCriteria criteria,

        CancellationToken cancellationToken = default);



    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);



    Task SaveAsync(Payment payment, CancellationToken cancellationToken = default);



    Task<IReadOnlyList<PaymentMethod>> GetActiveMethodsAsync(int storeId, CancellationToken cancellationToken = default);



    Task<PaymentMethod?> GetMethodByIdAsync(int id, CancellationToken cancellationToken = default);



    Task<PaymentMethod?> GetMethodBySystemNameAsync(int storeId, string systemName, CancellationToken cancellationToken = default);



    Task<IReadOnlyList<PaymentMethod>> ListMethodsAsync(int? storeId, CancellationToken cancellationToken = default);



    Task AddMethodAsync(PaymentMethod method, CancellationToken cancellationToken = default);



    Task SaveMethodAsync(PaymentMethod method, CancellationToken cancellationToken = default);



    Task<PaymentCallbackRecord?> GetCallbackRecordAsync(

        string providerSystemName,

        string callbackKey,

        CancellationToken cancellationToken = default);



    Task AddCallbackRecordAsync(PaymentCallbackRecord record, CancellationToken cancellationToken = default);

    Task<Refund?> GetRefundByIdempotencyKeyAsync(
        int paymentId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}



public sealed record PaymentListCriteria(

    int Page,

    int PageSize,

    int? StoreId,

    int? OrderId,

    PaymentStatus? Status,

    DateTime? CreatedFromUtc,

    DateTime? CreatedToUtc);

