using Commerce.Framework.Core.Errors;

namespace Commerce.Framework.Core.Results;

public static class ResultExtensions
{
    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return result.IsSuccess
            ? Result.Success(mapper(result.Value!))
            : Result.Failure<TOut>(result.Error!);
    }

    public static async Task<Result<TOut>> MapAsync<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Task<TOut>> mapper,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        if (result.IsFailure)
        {
            return Result.Failure<TOut>(result.Error!);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var mapped = await mapper(result.Value!).ConfigureAwait(false);
        return Result.Success(mapped);
    }

    public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        return result.IsSuccess
            ? binder(result.Value!)
            : Result.Failure<TOut>(result.Error!);
    }

    public static TOut Match<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, TOut> onSuccess,
        Func<Error, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return result.IsSuccess
            ? onSuccess(result.Value!)
            : onFailure(result.Error!);
    }

    public static Result<T> Ensure<T>(
        this Result<T> result,
        Func<T, bool> predicate,
        Error error)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(error);

        if (result.IsFailure)
        {
            return result;
        }

        return predicate(result.Value!)
            ? result
            : Result.Failure<T>(error);
    }
}
