using FluentResults;

namespace Utility.Shared.ServiceHandler;

public interface IServiceHandlerAsync<TParams, TResult>
{
    Task<Result<TResult>> HandleAsync(TParams @params);
}

public interface IServiceHandlerAsync<TResult>
{
    Task<Result<TResult>> HandleAsync();
}

public interface IServiceHandlerVoidAsync<TParams>
{
    Task<Result> HandleAsync(TParams @params);
}

public interface IServiceHandler<TParams, TResult>
{
    Result<TResult> Handle(TParams @params);
}

public interface IServiceHandlerVoid<TParams>
{
    Result Handle(TParams @params);
}