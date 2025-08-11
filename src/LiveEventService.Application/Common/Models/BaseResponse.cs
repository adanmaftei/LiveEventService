namespace LiveEventService.Application.Common.Models;

public class BaseResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public IEnumerable<string>? Errors { get; set; }

    public static BaseResponse Succeeded(string? message = null)
    {
        return new BaseResponse { Success = true, Message = message };
    }

    public static BaseResponse Failed(string message, IEnumerable<string>? errors = null)
    {
        return new BaseResponse { Success = false, Message = message, Errors = errors };
    }
}

public class BaseResponse<T> : BaseResponse
{
    public T? Data { get; set; }

    public static BaseResponse<T> Succeeded(T data, string? message = null)
    {
        return new BaseResponse<T> { Success = true, Data = data, Message = message };
    }

    public new static BaseResponse<T> Failed(string message, IEnumerable<string>? errors = null)
    {
        return new BaseResponse<T> { Success = false, Message = message, Errors = errors };
    }
}
