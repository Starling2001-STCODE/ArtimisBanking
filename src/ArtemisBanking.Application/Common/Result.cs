namespace ArtemisBanking.Application.Common;

public class Result
{
    public bool Succeeded { get; protected init; }
    public string? Error { get; protected init; }

    public static Result Success() => new() { Succeeded = true };
    public static Result Failure(string error) => new() { Succeeded = false, Error = error };
}

public class Result<T> : Result
{
    public T Data { get; protected init; } = default!; 

    public static Result<T> Success(T data) => new()
    {
        Succeeded = true,
        Data = data
    };

    public static new Result<T> Failure(string error) => new()
    {
        Succeeded = false,
        Error = error,
        Data = default!
    };
}
