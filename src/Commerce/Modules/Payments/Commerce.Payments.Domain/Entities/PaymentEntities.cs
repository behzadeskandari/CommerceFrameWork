using Commerce.Framework.Core.Entities;

using Commerce.Payments.Domain.Enums;



namespace Commerce.Payments.Domain.Entities;



public sealed class Payment : AggregateRoot

{

    public const int CurrencyMaxLength = 8;

    public const int ProviderSystemNameMaxLength = 128;

    public const int ProviderPaymentIdMaxLength = 256;

    public const int MetadataMaxLength = 8000;

    public const int IdempotencyKeyMaxLength = 128;



    private readonly List<PaymentTransaction> _transactions = [];

    private readonly List<PaymentAttempt> _attempts = [];

    private readonly List<Refund> _refunds = [];



    private Payment()

    {

    }



    public int StoreId { get; private set; }



    public int OrderId { get; private set; }



    public int? CustomerId { get; private set; }



    public string Currency { get; private set; } = string.Empty;



    public decimal Amount { get; private set; }



    public PaymentStatus Status { get; private set; }



    public string ProviderSystemName { get; private set; } = string.Empty;



    public string? ProviderPaymentId { get; private set; }



    public DateTime CreatedAtUtc { get; private set; }



    public DateTime UpdatedAtUtc { get; private set; }



    public string? Metadata { get; private set; }



    public string? IdempotencyKey { get; private set; }



    public decimal RefundedAmount { get; private set; }



    public IReadOnlyCollection<PaymentTransaction> Transactions => _transactions;



    public IReadOnlyCollection<PaymentAttempt> Attempts => _attempts;



    public IReadOnlyCollection<Refund> Refunds => _refunds;



    public static Payment Create(

        int storeId,

        int orderId,

        int? customerId,

        string currency,

        decimal amount,

        string providerSystemName,

        string? idempotencyKey = null,

        string? metadata = null)

    {

        ValidateStore(storeId);

        ValidateOrder(orderId);

        ValidateAmount(amount);

        ValidateProvider(providerSystemName);

        ValidateCurrency(currency);



        var utcNow = DateTime.UtcNow;

        return new Payment

        {

            StoreId = storeId,

            OrderId = orderId,

            CustomerId = customerId,

            Currency = currency.Trim().ToUpperInvariant(),

            Amount = amount,

            Status = PaymentStatus.Pending,

            ProviderSystemName = providerSystemName.Trim(),

            IdempotencyKey = NormalizeOptional(idempotencyKey, IdempotencyKeyMaxLength),

            Metadata = NormalizeOptional(metadata, MetadataMaxLength),

            RefundedAmount = 0m,

            CreatedAtUtc = utcNow,

            UpdatedAtUtc = utcNow

        };

    }



    public PaymentAttempt StartAttempt(int attemptNumber)

    {

        var attempt = PaymentAttempt.Create(Id, attemptNumber);

        _attempts.Add(attempt);

        Touch();

        return attempt;

    }



    public PaymentTransaction AddTransaction(

        PaymentTransactionType transactionType,

        decimal amount,

        string currency,

        PaymentTransactionStatus status,

        string? providerTransactionId = null,

        string? requestReference = null,

        string? responseReference = null,

        string? failureCode = null,

        string? failureMessage = null)

    {

        var transaction = PaymentTransaction.Create(

            Id,

            transactionType,

            amount,

            currency,

            status,

            providerTransactionId,

            requestReference,

            responseReference,

            failureCode,

            failureMessage);

        _transactions.Add(transaction);

        Touch();

        return transaction;

    }



    public void MarkInitiated(string? providerPaymentId = null)

    {

        EnsureStatus(PaymentStatus.Pending);

        Status = PaymentStatus.Initiated;

        ProviderPaymentId = NormalizeOptional(providerPaymentId, ProviderPaymentIdMaxLength);

        Touch();

    }



    public void MarkRedirectRequired(string? providerPaymentId = null)

    {

        EnsureStatus(PaymentStatus.Pending, PaymentStatus.Initiated);

        Status = PaymentStatus.RedirectRequired;

        ProviderPaymentId = NormalizeOptional(providerPaymentId, ProviderPaymentIdMaxLength) ?? ProviderPaymentId;

        Touch();

    }



    public void MarkAuthorized(string? providerPaymentId = null)

    {

        EnsureStatus(PaymentStatus.Pending, PaymentStatus.Initiated, PaymentStatus.RedirectRequired);

        Status = PaymentStatus.Authorized;

        ProviderPaymentId = NormalizeOptional(providerPaymentId, ProviderPaymentIdMaxLength) ?? ProviderPaymentId;

        Touch();

    }



    public void MarkCaptured(string? providerPaymentId = null)

    {

        EnsureStatus(

            PaymentStatus.Pending,

            PaymentStatus.Initiated,

            PaymentStatus.RedirectRequired,

            PaymentStatus.Authorized);

        Status = PaymentStatus.Captured;

        ProviderPaymentId = NormalizeOptional(providerPaymentId, ProviderPaymentIdMaxLength) ?? ProviderPaymentId;

        Touch();

    }



    public void MarkFailed(string? failureMessage = null)

    {

        if (Status is PaymentStatus.Captured or PaymentStatus.Refunded or PaymentStatus.PartiallyRefunded)

        {

            throw new InvalidOperationException($"Payment cannot fail from status {Status}.");

        }



        Status = PaymentStatus.Failed;

        if (!string.IsNullOrWhiteSpace(failureMessage))

        {

            Metadata = failureMessage.Trim();

        }



        Touch();

    }



    public void MarkCancelled(string? reason = null)

    {

        if (Status is PaymentStatus.Captured or PaymentStatus.Refunded or PaymentStatus.PartiallyRefunded)

        {

            throw new InvalidOperationException($"Payment cannot be cancelled from status {Status}.");

        }



        Status = PaymentStatus.Cancelled;

        if (!string.IsNullOrWhiteSpace(reason))

        {

            Metadata = reason.Trim();

        }



        Touch();

    }



    public Refund ApplyRefund(decimal amount, string currency, string? reason = null, string? idempotencyKey = null)

    {

        if (amount <= 0)

        {

            throw new ArgumentOutOfRangeException(nameof(amount));

        }



        if (Status is not PaymentStatus.Captured and not PaymentStatus.PartiallyRefunded)

        {

            throw new InvalidOperationException($"Refund is not allowed from status {Status}.");

        }



        if (RefundedAmount + amount > Amount)

        {

            throw new InvalidOperationException("Refund amount exceeds remaining payment balance.");

        }



        var refund = Refund.Create(Id, amount, currency, reason, idempotencyKey);

        _refunds.Add(refund);

        RefundedAmount += amount;

        Status = RefundedAmount >= Amount ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;

        Touch();

        return refund;

    }



    public void UpdateMetadata(string? metadata) => Metadata = NormalizeOptional(metadata, MetadataMaxLength);



    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;



    private void EnsureStatus(params PaymentStatus[] allowed)

    {

        if (!allowed.Contains(Status))

        {

            throw new InvalidOperationException($"Invalid payment status transition from {Status}.");

        }

    }



    private static void ValidateStore(int storeId)

    {

        if (storeId <= 0)

        {

            throw new ArgumentOutOfRangeException(nameof(storeId));

        }

    }



    private static void ValidateOrder(int orderId)

    {

        if (orderId <= 0)

        {

            throw new ArgumentOutOfRangeException(nameof(orderId));

        }

    }



    private static void ValidateAmount(decimal amount)

    {

        if (amount < 0)

        {

            throw new ArgumentOutOfRangeException(nameof(amount));

        }

    }



    private static void ValidateProvider(string providerSystemName)

    {

        if (string.IsNullOrWhiteSpace(providerSystemName))

        {

            throw new ArgumentException("Provider system name is required.", nameof(providerSystemName));

        }

    }



    private static void ValidateCurrency(string currency)

    {

        if (string.IsNullOrWhiteSpace(currency))

        {

            throw new ArgumentException("Currency is required.", nameof(currency));

        }

    }



    private static string? NormalizeOptional(string? value, int maxLength)

    {

        if (string.IsNullOrWhiteSpace(value))

        {

            return null;

        }



        var trimmed = value.Trim();

        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;

    }

}



public sealed class PaymentTransaction : Entity

{

    public const int CurrencyMaxLength = 8;

    public const int ProviderTransactionIdMaxLength = 256;

    public const int ReferenceMaxLength = 256;

    public const int FailureCodeMaxLength = 64;

    public const int FailureMessageMaxLength = 1000;



    private PaymentTransaction()

    {

    }



    public int PaymentId { get; private set; }



    public PaymentTransactionType TransactionType { get; private set; }



    public decimal Amount { get; private set; }



    public string Currency { get; private set; } = string.Empty;



    public string? ProviderTransactionId { get; private set; }



    public PaymentTransactionStatus Status { get; private set; }



    public string? RequestReference { get; private set; }



    public string? ResponseReference { get; private set; }



    public string? FailureCode { get; private set; }



    public string? FailureMessage { get; private set; }



    public DateTime CreatedAtUtc { get; private set; }



    public static PaymentTransaction Create(

        int paymentId,

        PaymentTransactionType transactionType,

        decimal amount,

        string currency,

        PaymentTransactionStatus status,

        string? providerTransactionId = null,

        string? requestReference = null,

        string? responseReference = null,

        string? failureCode = null,

        string? failureMessage = null)

    {

        if (paymentId < 0)

        {

            throw new ArgumentOutOfRangeException(nameof(paymentId));

        }



        if (amount < 0)

        {

            throw new ArgumentOutOfRangeException(nameof(amount));

        }



        if (string.IsNullOrWhiteSpace(currency))

        {

            throw new ArgumentException("Currency is required.", nameof(currency));

        }



        return new PaymentTransaction

        {

            PaymentId = paymentId,

            TransactionType = transactionType,

            Amount = amount,

            Currency = currency.Trim().ToUpperInvariant(),

            ProviderTransactionId = TrimOptional(providerTransactionId, ProviderTransactionIdMaxLength),

            Status = status,

            RequestReference = TrimOptional(requestReference, ReferenceMaxLength),

            ResponseReference = TrimOptional(responseReference, ReferenceMaxLength),

            FailureCode = TrimOptional(failureCode, FailureCodeMaxLength),

            FailureMessage = TrimOptional(failureMessage, FailureMessageMaxLength),

            CreatedAtUtc = DateTime.UtcNow

        };

    }



    private static string? TrimOptional(string? value, int maxLength)

    {

        if (string.IsNullOrWhiteSpace(value))

        {

            return null;

        }



        var trimmed = value.Trim();

        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;

    }

}



public sealed class PaymentMethod : AggregateRoot

{

    public const int NameMaxLength = 200;

    public const int SystemNameMaxLength = 128;

    public const int ProviderSystemNameMaxLength = 128;

    public const int DisplayNameMaxLength = 200;

    public const int ConfigurationJsonMaxLength = 8000;



    private PaymentMethod()

    {

    }



    public int StoreId { get; private set; }



    public string Name { get; private set; } = string.Empty;



    public string SystemName { get; private set; } = string.Empty;



    public string ProviderSystemName { get; private set; } = string.Empty;



    public string DisplayName { get; private set; } = string.Empty;



    public bool IsActive { get; private set; }



    public int DisplayOrder { get; private set; }



    public bool RequiresRedirect { get; private set; }



    public bool SupportsGuest { get; private set; }



    public bool SupportsFreeOrders { get; private set; }



    public string? ConfigurationJson { get; private set; }



    public bool IsDeleted { get; private set; }



    public DateTime CreatedAtUtc { get; private set; }



    public DateTime UpdatedAtUtc { get; private set; }



    public static PaymentMethod Create(

        int storeId,

        string name,

        string systemName,

        string providerSystemName,

        string displayName,

        bool isActive,

        int displayOrder,

        bool requiresRedirect,

        bool supportsGuest,

        bool supportsFreeOrders,

        string? configurationJson = null)

    {

        ValidateStore(storeId);

        ValidateName(name);

        ValidateSystemName(systemName);

        ValidateProvider(providerSystemName);

        ValidateDisplayName(displayName);



        var utcNow = DateTime.UtcNow;

        return new PaymentMethod

        {

            StoreId = storeId,

            Name = name.Trim(),

            SystemName = systemName.Trim().ToLowerInvariant(),

            ProviderSystemName = providerSystemName.Trim(),

            DisplayName = displayName.Trim(),

            IsActive = isActive,

            DisplayOrder = displayOrder,

            RequiresRedirect = requiresRedirect,

            SupportsGuest = supportsGuest,

            SupportsFreeOrders = supportsFreeOrders,

            ConfigurationJson = TrimOptional(configurationJson, ConfigurationJsonMaxLength),

            IsDeleted = false,

            CreatedAtUtc = utcNow,

            UpdatedAtUtc = utcNow

        };

    }



    public void Update(

        string name,

        string displayName,

        bool isActive,

        int displayOrder,

        bool requiresRedirect,

        bool supportsGuest,

        bool supportsFreeOrders,

        string? configurationJson = null)

    {

        EnsureNotDeleted();

        ValidateName(name);

        ValidateDisplayName(displayName);

        Name = name.Trim();

        DisplayName = displayName.Trim();

        IsActive = isActive;

        DisplayOrder = displayOrder;

        RequiresRedirect = requiresRedirect;

        SupportsGuest = supportsGuest;

        SupportsFreeOrders = supportsFreeOrders;

        ConfigurationJson = TrimOptional(configurationJson, ConfigurationJsonMaxLength);

        Touch();

    }



    public void SoftDelete()

    {

        IsDeleted = true;

        IsActive = false;

        Touch();

    }



    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;



    private void EnsureNotDeleted()

    {

        if (IsDeleted)

        {

            throw new InvalidOperationException("Payment method has been deleted.");

        }

    }



    private static void ValidateStore(int storeId)

    {

        if (storeId <= 0)

        {

            throw new ArgumentOutOfRangeException(nameof(storeId));

        }

    }



    private static void ValidateName(string name)

    {

        if (string.IsNullOrWhiteSpace(name))

        {

            throw new ArgumentException("Name is required.", nameof(name));

        }

    }



    private static void ValidateSystemName(string systemName)

    {

        if (string.IsNullOrWhiteSpace(systemName))

        {

            throw new ArgumentException("System name is required.", nameof(systemName));

        }

    }



    private static void ValidateProvider(string providerSystemName)

    {

        if (string.IsNullOrWhiteSpace(providerSystemName))

        {

            throw new ArgumentException("Provider system name is required.", nameof(providerSystemName));

        }

    }



    private static void ValidateDisplayName(string displayName)

    {

        if (string.IsNullOrWhiteSpace(displayName))

        {

            throw new ArgumentException("Display name is required.", nameof(displayName));

        }

    }



    private static string? TrimOptional(string? value, int maxLength)

    {

        if (string.IsNullOrWhiteSpace(value))

        {

            return null;

        }



        var trimmed = value.Trim();

        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;

    }

}



public sealed class PaymentAttempt : Entity

{

    public const int FailureMessageMaxLength = 1000;



    private PaymentAttempt()

    {

    }



    public int PaymentId { get; private set; }



    public int AttemptNumber { get; private set; }



    public PaymentAttemptStatus Status { get; private set; }



    public DateTime CreatedAtUtc { get; private set; }



    public string? FailureMessage { get; private set; }



    public static PaymentAttempt Create(int paymentId, int attemptNumber)

    {

        if (paymentId < 0)

        {

            throw new ArgumentOutOfRangeException(nameof(paymentId));

        }



        if (attemptNumber <= 0)

        {

            throw new ArgumentOutOfRangeException(nameof(attemptNumber));

        }



        return new PaymentAttempt

        {

            PaymentId = paymentId,

            AttemptNumber = attemptNumber,

            Status = PaymentAttemptStatus.Pending,

            CreatedAtUtc = DateTime.UtcNow

        };

    }



    public void MarkSucceeded()

    {

        Status = PaymentAttemptStatus.Succeeded;

    }



    public void MarkFailed(string? failureMessage = null)

    {

        Status = PaymentAttemptStatus.Failed;

        FailureMessage = TrimOptional(failureMessage, FailureMessageMaxLength);

    }



    private static string? TrimOptional(string? value, int maxLength)

    {

        if (string.IsNullOrWhiteSpace(value))

        {

            return null;

        }



        var trimmed = value.Trim();

        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;

    }

}



public sealed class Refund : AggregateRoot

{

    public const int CurrencyMaxLength = 8;

    public const int ReasonMaxLength = 500;
    public const int IdempotencyKeyMaxLength = 128;

    private readonly List<RefundTransaction> _transactions = [];



    private Refund()

    {

    }



    public int PaymentId { get; private set; }



    public decimal Amount { get; private set; }



    public string Currency { get; private set; } = string.Empty;



    public RefundStatus Status { get; private set; }



    public string? Reason { get; private set; }

    public string? IdempotencyKey { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }



    public IReadOnlyCollection<RefundTransaction> Transactions => _transactions;



    public static Refund Create(int paymentId, decimal amount, string currency, string? reason = null, string? idempotencyKey = null)

    {

        if (paymentId < 0)

        {

            throw new ArgumentOutOfRangeException(nameof(paymentId));

        }



        if (amount <= 0)

        {

            throw new ArgumentOutOfRangeException(nameof(amount));

        }



        if (string.IsNullOrWhiteSpace(currency))

        {

            throw new ArgumentException("Currency is required.", nameof(currency));

        }



        return new Refund

        {

            PaymentId = paymentId,

            Amount = amount,

            Currency = currency.Trim().ToUpperInvariant(),

            Status = RefundStatus.Pending,

            Reason = TrimOptional(reason, ReasonMaxLength),

            IdempotencyKey = TrimOptional(idempotencyKey, IdempotencyKeyMaxLength),

            CreatedAtUtc = DateTime.UtcNow

        };

    }



    public RefundTransaction AddTransaction(

        decimal amount,

        RefundStatus status,

        int? paymentTransactionId = null,

        string? providerTransactionId = null)

    {

        var transaction = RefundTransaction.Create(Id, paymentTransactionId, amount, providerTransactionId, status);

        _transactions.Add(transaction);

        if (status == RefundStatus.Succeeded)

        {

            Status = RefundStatus.Succeeded;

        }

        else if (status == RefundStatus.Failed)

        {

            Status = RefundStatus.Failed;

        }



        return transaction;

    }



    public void MarkSucceeded() => Status = RefundStatus.Succeeded;



    public void MarkFailed() => Status = RefundStatus.Failed;



    private static string? TrimOptional(string? value, int maxLength)

    {

        if (string.IsNullOrWhiteSpace(value))

        {

            return null;

        }



        var trimmed = value.Trim();

        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;

    }

}



public sealed class RefundTransaction : Entity

{

    public const int ProviderTransactionIdMaxLength = 256;



    private RefundTransaction()

    {

    }



    public int RefundId { get; private set; }



    public int? PaymentTransactionId { get; private set; }



    public decimal Amount { get; private set; }



    public string? ProviderTransactionId { get; private set; }



    public RefundStatus Status { get; private set; }



    public DateTime CreatedAtUtc { get; private set; }



    public static RefundTransaction Create(

        int refundId,

        int? paymentTransactionId,

        decimal amount,

        string? providerTransactionId,

        RefundStatus status)

    {

        if (refundId < 0)

        {

            throw new ArgumentOutOfRangeException(nameof(refundId));

        }



        if (amount <= 0)

        {

            throw new ArgumentOutOfRangeException(nameof(amount));

        }



        return new RefundTransaction

        {

            RefundId = refundId,

            PaymentTransactionId = paymentTransactionId,

            Amount = amount,

            ProviderTransactionId = TrimOptional(providerTransactionId, ProviderTransactionIdMaxLength),

            Status = status,

            CreatedAtUtc = DateTime.UtcNow

        };

    }



    private static string? TrimOptional(string? value, int maxLength)

    {

        if (string.IsNullOrWhiteSpace(value))

        {

            return null;

        }



        var trimmed = value.Trim();

        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;

    }

}



public sealed class PaymentCallbackRecord : Entity

{

    public const int ProviderSystemNameMaxLength = 128;

    public const int CallbackKeyMaxLength = 256;

    public const int PayloadHashMaxLength = 128;



    private PaymentCallbackRecord()

    {

    }



    public string ProviderSystemName { get; private set; } = string.Empty;



    public string CallbackKey { get; private set; } = string.Empty;



    public string PayloadHash { get; private set; } = string.Empty;



    public DateTime ProcessedAtUtc { get; private set; }



    public int? PaymentId { get; private set; }



    public static PaymentCallbackRecord Create(

        string providerSystemName,

        string callbackKey,

        string payloadHash,

        int? paymentId = null)

    {

        if (string.IsNullOrWhiteSpace(providerSystemName))

        {

            throw new ArgumentException("Provider system name is required.", nameof(providerSystemName));

        }



        if (string.IsNullOrWhiteSpace(callbackKey))

        {

            throw new ArgumentException("Callback key is required.", nameof(callbackKey));

        }



        if (string.IsNullOrWhiteSpace(payloadHash))

        {

            throw new ArgumentException("Payload hash is required.", nameof(payloadHash));

        }



        return new PaymentCallbackRecord

        {

            ProviderSystemName = providerSystemName.Trim(),

            CallbackKey = callbackKey.Trim(),

            PayloadHash = payloadHash.Trim(),

            PaymentId = paymentId,

            ProcessedAtUtc = DateTime.UtcNow

        };

    }

}

