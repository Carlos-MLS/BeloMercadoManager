namespace BeloMercado.Application.Common;

public enum OperationStatus
{
    Ok,
    NotFound,
    Conflict,
    Validation,
    Forbidden
}

public class OperationResult<T>
{
    public OperationStatus Status { get; init; }
    public T? Value { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public bool IsSuccess => Status == OperationStatus.Ok;

    public static OperationResult<T> Ok(T value) =>
        new() { Status = OperationStatus.Ok, Value = value };

    public static OperationResult<T> NotFound(string code, string message) =>
        new() { Status = OperationStatus.NotFound, ErrorCode = code, ErrorMessage = message };

    public static OperationResult<T> Conflict(string code, string message) =>
        new() { Status = OperationStatus.Conflict, ErrorCode = code, ErrorMessage = message };

    public static OperationResult<T> Validation(string code, string message) =>
        new() { Status = OperationStatus.Validation, ErrorCode = code, ErrorMessage = message };

    public static OperationResult<T> Forbidden(string code, string message) =>
        new() { Status = OperationStatus.Forbidden, ErrorCode = code, ErrorMessage = message };
}
