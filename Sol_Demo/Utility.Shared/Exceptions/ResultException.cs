using FluentResults;
using Models.Shared.Constant;
using System.Net;

namespace Utility.Shared.Exceptions;

public static class ResultExceptionFactory
{
    public static Result<T> Error<T>(string message, HttpStatusCode httpStatusCode)
    {
        return Result.Fail<T>(new FluentResults.Error(message).WithMetadata(ConstantValue.StatusCode, httpStatusCode));
    }

    public static Result Error(string message, HttpStatusCode httpStatusCode)
    {
        return Result.Fail(new FluentResults.Error(message).WithMetadata(ConstantValue.StatusCode, httpStatusCode));
    }

    public static Result<T> Error<T>(FluentResults.Error error)
    {
        return Result.Fail<T>(error);
    }

    public static Result Error(FluentResults.Error error)
    {
        return Result.Fail(error);
    }

    public static Result<T> Error<T>(FluentResults.IError error)
    {
        return Result.Fail<T>(error);
    }

    public static Result Error(FluentResults.IError error)
    {
        return Result.Fail(error);
    }
}