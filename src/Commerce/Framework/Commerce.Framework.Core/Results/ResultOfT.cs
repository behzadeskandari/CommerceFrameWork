using Commerce.Framework.Core.Errors;

namespace Commerce.Framework.Core.Results;

public sealed class Result<T> : Result
{
    private Result(T value)
        : base(true, null)
    {
        Value = value;
    }

    private Result(Error error)
        : base(false, error)
    {
        Value = default;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(value);

    public new static Result<T> Failure(Error error) => new(error);
}
