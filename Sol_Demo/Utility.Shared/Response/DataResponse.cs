using Models.Shared.Responses;

namespace Utility.Shared.Response;

public static class DataResponse
{
    public static DataResponse<TData> Success<TData>(int? statusCode, TData? data, string? message = default)
    {
        return new DataResponse<TData> { Success = true, StatusCode = statusCode, Data = data, Message = message };
    }
}