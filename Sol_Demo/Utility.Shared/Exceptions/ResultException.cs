using FluentResults;
using Models.Shared.Constant;
using System.Net;

namespace Utility.Shared.Exceptions;

public static class ResultException
{
    public static Result<T> Error<T>(string message, HttpStatusCode httpStatusCode)
    {
        return Result.Fail<T>(new FluentResults.Error(message).WithMetadata(ConstantValue.StatusCode, httpStatusCode));
    }

    public static Result Error(string message, HttpStatusCode httpStatusCode)
    {
        return Result.Fail(new FluentResults.Error(message).WithMetadata(ConstantValue.StatusCode, httpStatusCode));
    }
}