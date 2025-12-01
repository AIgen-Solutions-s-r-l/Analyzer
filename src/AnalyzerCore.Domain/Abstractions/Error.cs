namespace AnalyzerCore.Domain.Abstractions;

/// <summary>
/// Represents an error with a code and message.
/// Immutable and suitable for use in Result types.
/// </summary>
public sealed record Error(string Code, string Message)
{
    /// <summary>
    /// Represents no error (success state).
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>
    /// Represents a null value error.
    /// </summary>
    public static readonly Error NullValue = new("Error.NullValue", "A null value was provided.");

    /// <summary>
    /// Creates a validation error with the specified message.
    /// </summary>
    public static Error Validation(string message) => new("Error.Validation", message);

    /// <summary>
    /// Creates a not found error with the specified message.
    /// </summary>
    public static Error NotFound(string message) => new("Error.NotFound", message);

    /// <summary>
    /// Creates a conflict error with the specified message.
    /// </summary>
    public static Error Conflict(string message) => new("Error.Conflict", message);

    /// <summary>
    /// Creates a failure error with the specified message.
    /// </summary>
    public static Error Failure(string message) => new("Error.Failure", message);

    /// <summary>
    /// Implicit conversion to string returns the error message.
    /// </summary>
    public static implicit operator string(Error error) => error.Message;

    public override string ToString() => $"[{Code}] {Message}";
}

/// <summary>
/// Represents a validation error with multiple messages.
/// </summary>
public sealed record ValidationError : Error
{
    public IReadOnlyCollection<string> Errors { get; }

    public ValidationError(IEnumerable<string> errors)
        : base("Error.Validation", "One or more validation errors occurred.")
    {
        Errors = errors.ToList().AsReadOnly();
    }

    public ValidationError(params string[] errors)
        : this(errors.AsEnumerable())
    {
    }
}
