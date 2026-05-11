namespace GearNXT_Backend.Services;

public class ServiceResult<T>
{
    public bool Success { get; }
    public int StatusCode { get; }
    public string? Message { get; }
    public T? Data { get; }

    private ServiceResult(bool success, int statusCode, string? message, T? data)
    {
        Success = success;
        StatusCode = statusCode;
        Message = message;
        Data = data;
    }

    public static ServiceResult<T> Ok(T data, int statusCode = 200)
    {
        return new ServiceResult<T>(true, statusCode, null, data);
    }

    public static ServiceResult<T> Fail(int statusCode, string message)
    {
        return new ServiceResult<T>(false, statusCode, message, default);
    }
}
