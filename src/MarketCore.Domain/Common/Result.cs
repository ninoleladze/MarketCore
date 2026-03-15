namespace MarketCore.Domain.Common;

public interface IResultBase
{
    bool IsFailure { get; }
    string? Error { get; }
}

public sealed class Result<T> : IResultBase
{

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T? Value { get; }

    public string? Error { get; }

    public static Result<T> Success(T value) => new(true, value, null);

    public static Result<T> Failure(string error) => new(false, default, error);

    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
    {
        return IsSuccess
            ? Result<TOut>.Success(mapper(Value!))
            : Result<TOut>.Failure(Error!);
    }

    public Result ToResult()
    {
        return IsSuccess ? Result.Success() : Result.Failure(Error!);
    }

    public override string ToString() =>
        IsSuccess ? $"Success({Value})" : $"Failure({Error})";
}

public sealed class Result : IResultBase
{

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public string? Error { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(string error) => new(false, error);

    private Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public Result<T> WithValue<T>(T value)
    {
        return IsSuccess
            ? Result<T>.Success(value)
            : Result<T>.Failure(Error!);
    }

    public override string ToString() =>
        IsSuccess ? "Success" : $"Failure({Error})";
}
