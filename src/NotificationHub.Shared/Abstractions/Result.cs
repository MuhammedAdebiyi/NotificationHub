namespace NotificationHub.Shared.Abstractions;

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    /// <summary>
    /// Stable machine-readable failure code (e.g. "invalid_reply_to") so API
    /// callers can branch on the failure instead of matching message text.
    /// Null for failures that only carry a human-readable message.
    /// </summary>
    public string? Code { get; }

    protected Result(bool isSuccess, string? error, string? code = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        Code = code;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error, string? code = null) => new(false, error, code);
}

public class Result<T> : Result
{
    public T? Value { get; }

    protected Result(bool isSuccess, T? value, string? error, string? code = null)
        : base(isSuccess, error, code)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static new Result<T> Failure(string error, string? code = null) => new(false, default, error, code);
}
