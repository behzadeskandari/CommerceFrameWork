using Commerce.Payments.Contracts.Payments;

using Commerce.Payments.Domain.Entities;

using Commerce.Payments.Domain.Enums;



namespace Commerce.Payments.Application.Payments;



internal static class PaymentMapper

{

    internal static PaymentDto ToDto(Payment payment) =>

        new(

            payment.Id,

            payment.StoreId,

            payment.OrderId,

            payment.CustomerId,

            payment.Currency,

            payment.Amount,

            payment.Status,

            payment.ProviderSystemName,

            payment.ProviderPaymentId,

            payment.RefundedAmount,

            payment.CreatedAtUtc,

            payment.UpdatedAtUtc);



    internal static PaymentDetailDto ToDetail(Payment payment) =>

        new(

            ToDto(payment),

            payment.Transactions.Select(ToTransactionDto).ToList(),

            payment.Attempts.Select(ToAttemptDto).ToList(),

            payment.Refunds.Select(ToRefundDto).ToList());



    internal static PaymentTransactionDto ToTransactionDto(PaymentTransaction transaction) =>

        new(

            transaction.Id,

            transaction.TransactionType,

            transaction.Amount,

            transaction.Currency,

            transaction.Status,

            transaction.ProviderTransactionId,

            transaction.FailureCode,

            transaction.FailureMessage,

            transaction.CreatedAtUtc);



    internal static PaymentAttemptDto ToAttemptDto(PaymentAttempt attempt) =>

        new(

            attempt.Id,

            attempt.AttemptNumber,

            attempt.Status,

            attempt.FailureMessage,

            attempt.CreatedAtUtc);



    internal static RefundDto ToRefundDto(Refund refund) =>

        new(

            refund.Id,

            refund.Amount,

            refund.Currency,

            refund.Status,

            refund.Reason,

            refund.CreatedAtUtc);



    internal static CreatePaymentResultDto ToCreateResult(Payment payment, PaymentResult? providerResult = null) =>

        new(

            payment.Id,

            payment.Status,

            providerResult?.RedirectUrl,

            providerResult?.Instructions);

}

