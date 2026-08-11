namespace Commerce.Framework.Core.Errors;

public enum ErrorCode
{
    None = 0,
    ValidationFailed = 1000,
    NotFound = 2000,
    Conflict = 3000,
    Unauthorized = 4000,
    Forbidden = 4001,
    OperationFailed = 5000,
    InvalidArgument = 5001
}
