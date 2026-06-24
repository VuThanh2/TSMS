namespace SharedKernel.Primitives;

/// Represents the outcome of an operation that can either succeed or fail with an Error.
/// Use Result for void operations, Result<T> for operations that return a value.
public class Result {
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; } = Error.None;
 
    protected Result(bool isSuccess, Error error) {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("A successful result cannot carry an error.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("A failed result must carry an error.");
 
        IsSuccess = isSuccess;
        Error = error;
    }
 
    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
 
    public static Result<TValue> Success<TValue>(TValue value) => Result<TValue>.Success(value);
    public static Result<TValue> Failure<TValue>(Error error) => Result<TValue>.Failure(error);
}
 
/// Represents the outcome of an operation that returns a value of type TValue on success.
public sealed class Result<TValue> : Result {
    private readonly TValue? _value;
 
    private Result(bool isSuccess, Error error, TValue? value) : base(isSuccess, error) {
        _value = value;
    }
 
    /// Returns the value if the result is successful.
    /// Throws InvalidOperationException if accessed on a failed result.
    public TValue Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException("Cannot access Value on a failed result.");
 
    public static Result<TValue> Success(TValue value) => new(true, Error.None, value);
    public static new Result<TValue> Failure(Error error) => new(false, error, default);
}