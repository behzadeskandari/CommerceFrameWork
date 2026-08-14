namespace Commerce.Payments.Domain.Enums;



public enum PaymentStatus

{

    Pending = 0,

    Initiated = 1,

    RedirectRequired = 2,

    Authorized = 3,

    Captured = 4,

    Failed = 5,

    Cancelled = 6,

    PartiallyRefunded = 7,

    Refunded = 8

}



public enum PaymentTransactionStatus

{

    Pending = 0,

    Succeeded = 1,

    Failed = 2

}



public enum PaymentTransactionType

{

    Authorization = 0,

    Capture = 1,

    Sale = 2,

    Void = 3,

    Refund = 4,

    PartialRefund = 5,

    Verification = 6

}



public enum RefundStatus

{

    Pending = 0,

    Succeeded = 1,

    Failed = 2,

    Cancelled = 3

}



public enum PaymentProviderType

{

    Manual = 0,

    Redirect = 1,

    Hosted = 2,

    Offline = 3

}



public enum PaymentAttemptStatus

{

    Pending = 0,

    Succeeded = 1,

    Failed = 2

}

