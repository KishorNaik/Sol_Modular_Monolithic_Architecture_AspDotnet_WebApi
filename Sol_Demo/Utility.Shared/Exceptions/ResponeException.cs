using Models.Shared.Responses;
using Utility.Shared.Response;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Utility.Shared.Exceptions;

public static class ResponeException
{
    public static DataResponse<T> Error<T>(string message, int statusCode, T data = default)
    {
        return new DataResponse<T> { Success = false, StatusCode = statusCode, Data = data, Message = message };
    }
}