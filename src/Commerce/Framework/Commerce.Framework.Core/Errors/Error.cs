namespace Commerce.Framework.Core.Errors;

public sealed record Error
{
    public ErrorCode Code { get; init; }
    public string Message { get; init; }
    public ErrorType Type { get; init; }
    public string? Detail { get; init; }
    public IReadOnlyDictionary<string, object?> Metadata { get; init; }

    public Error(
        ErrorCode code,
        string message,
        ErrorType type = ErrorType.Failure,
        string? detail = null,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Error message is required.", nameof(message));
        }

        Code = code;
        Message = message;
        Type = type;
        Detail = detail;
        Metadata = metadata ?? new Dictionary<string, object?>();
    }

    public static Error Validation(string message, string? detail = null, IReadOnlyDictionary<string, object?>? metadata = null) =>
        new(ErrorCode.ValidationFailed, message, ErrorType.Validation, detail, metadata);

    public static Error NotFound(string message, string? detail = null) =>
        new(ErrorCode.NotFound, message, ErrorType.NotFound, detail);

    public static Error Conflict(string message, string? detail = null) =>
        new(ErrorCode.Conflict, message, ErrorType.Conflict, detail);

    public static Error Forbidden(string message, string? detail = null) =>
        new(ErrorCode.Forbidden, message, ErrorType.Forbidden, detail);

    public static Error Failure(string message, ErrorCode code = ErrorCode.OperationFailed, string? detail = null) =>
        new(code, message, ErrorType.Failure, detail);
}
