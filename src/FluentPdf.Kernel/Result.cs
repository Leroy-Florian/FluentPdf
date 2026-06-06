namespace FluentPdf.Kernel;

/// <summary>
/// Represents the outcome of an operation that can succeed or fail without throwing.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("A successful result cannot carry an error.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("A failed result must carry an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Whether the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Whether the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>The error describing the failure, or <see cref="Error.None"/> on success.</summary>
    public Error Error { get; }

    /// <summary>Creates a successful result.</summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>Creates a failed result.</summary>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>Creates a successful result carrying a value.</summary>
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    /// <summary>Creates a failed result of the given value type.</summary>
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

/// <summary>
/// Represents the outcome of an operation that yields a value on success.
/// </summary>
/// <typeparam name="TValue">The type of the produced value.</typeparam>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// The produced value. Accessing it on a failed result throws
    /// <see cref="InvalidOperationException"/>.
    /// </summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failed result cannot be accessed.");

    /// <summary>Wraps a value in a successful result.</summary>
    public static implicit operator Result<TValue>(TValue value) => Success(value);

    /// <summary>Wraps an error in a failed result.</summary>
    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);
}
