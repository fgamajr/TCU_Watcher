namespace TCUWatcher.Domain.Common;

public readonly record struct Result<TSuccess, TError>
{
    private readonly TSuccess? _value;
    private readonly TError?   _error;
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    private Result(TSuccess value) { IsSuccess = true;  _value = value;  _error = default; }
    private Result(TError   error) { IsSuccess = false; _error = error;  _value = default; }

    public static Result<TSuccess,TError> Success(TSuccess value) => new(value);
    public static Result<TSuccess,TError> Failure(TError error)   => new(error);

    public TSuccess Value => IsSuccess ? _value! : throw new InvalidOperationException();
    public TError   Error => IsFailure ? _error! : throw new InvalidOperationException();

    public TResult Match<TResult>(Func<TSuccess,TResult> ok, Func<TError,TResult> fail)
        => IsSuccess ? ok(Value) : fail(Error);
}
