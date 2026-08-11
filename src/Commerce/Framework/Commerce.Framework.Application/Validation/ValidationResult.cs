namespace Commerce.Framework.Application.Validation;

public sealed class ValidationResult
{
    private ValidationResult(bool isValid, IReadOnlyList<ValidationError> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    public bool IsValid { get; }

    public IReadOnlyList<ValidationError> Errors { get; }

    public static ValidationResult Success() => new(true, Array.Empty<ValidationError>());

    public static ValidationResult Failure(params ValidationError[] errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (errors.Length == 0)
        {
            throw new ArgumentException("At least one validation error is required.", nameof(errors));
        }

        return new ValidationResult(false, errors);
    }

    public static ValidationResult Failure(IEnumerable<ValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var materialized = errors.ToArray();
        if (materialized.Length == 0)
        {
            throw new ArgumentException("At least one validation error is required.", nameof(errors));
        }

        return new ValidationResult(false, materialized);
    }
}
