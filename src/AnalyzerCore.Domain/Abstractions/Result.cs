using System.Diagnostics.CodeAnalysis;

namespace AnalyzerCore.Domain.Abstractions;

/// <summary>
/// Represents the result of an operation that can either succeed or fail.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Cannot have error with success result.");

        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Cannot have no error with failure result.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    public static Result<TValue> Create<TValue>(TValue? value) =>
        value is not null ? Success(value) : Failure<TValue>(Error.NullValue);

    /// <summary>
    /// Creates a result from a condition.
    /// </summary>
    public static Result Ensure(bool condition, Error error) =>
        condition ? Success() : Failure(error);

    /// <summary>
    /// Combines multiple results into a single result.
    /// Returns failure if any of the results failed.
    /// </summary>
    public static Result Combine(params Result[] results)
    {
        foreach (var result in results)
        {
            if (result.IsFailure)
                return result;
        }

        return Success();
    }
}

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail with an error.
/// </summary>
/// <typeparam name="TValue">The type of the success value.</typeparam>
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the value if the result is successful.
    /// Throws InvalidOperationException if the result is a failure.
    /// </summary>
    [NotNull]
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of a failed result.");

    /// <summary>
    /// Gets the value or default if the result is a failure.
    /// </summary>
    public TValue? ValueOrDefault => IsSuccess ? _value : default;

    public static implicit operator Result<TValue>(TValue? value) =>
        value is not null ? Success(value) : Failure<TValue>(Error.NullValue);

    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);

    /// <summary>
    /// Maps the value to a new type if successful.
    /// </summary>
    public Result<TNew> Map<TNew>(Func<TValue, TNew> mapper) =>
        IsSuccess ? Result.Success(mapper(Value)) : Result.Failure<TNew>(Error);

    /// <summary>
    /// Binds to another result if successful.
    /// </summary>
    public Result<TNew> Bind<TNew>(Func<TValue, Result<TNew>> binder) =>
        IsSuccess ? binder(Value) : Result.Failure<TNew>(Error);

    /// <summary>
    /// Matches on success or failure and returns a value.
    /// </summary>
    public TResult Match<TResult>(
        Func<TValue, TResult> onSuccess,
        Func<Error, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value) : onFailure(Error);

    /// <summary>
    /// Executes an action on success.
    /// </summary>
    public Result<TValue> Tap(Action<TValue> action)
    {
        if (IsSuccess)
            action(Value);
        return this;
    }
}
