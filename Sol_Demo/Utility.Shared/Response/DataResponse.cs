using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Models.Shared.Responses;
using Utility.Shared.Traces;

namespace Utility.Shared.Response;

public interface IDataResponseFactory
{
    Task<DataResponse<TData>> Success<TData>(int? statusCode, TData? data, string? message = default);
}

public class DataResponseFactory : IDataResponseFactory
{
    private readonly ITraceIdService _traceIdService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DataResponseFactory(ITraceIdService traceIdService, IHttpContextAccessor httpContextAccessor)
    {
        _traceIdService = traceIdService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<DataResponse<TData>> Success<TData>(int? statusCode, TData? data, string? message = default)
    {
        var traceId = await _traceIdService.GetOrGenerateTraceId(_httpContextAccessor.HttpContext!); //traceId
        return new DataResponse<TData> { Success = true, StatusCode = statusCode, Data = data, Message = message, TraceId = traceId };
    }

    public async Task<DataResponse<T>> Error<T>(string message, int statusCode, T data = default)
    {
        var traceId = await _traceIdService.GetOrGenerateTraceId(_httpContextAccessor.HttpContext!); //traceId
        return new DataResponse<T> { Success = false, StatusCode = statusCode, Data = data, Message = message, TraceId = traceId };
    }
}

public static class DataResponseServiceExtension
{
    public static IServiceCollection AddDataResponse(this IServiceCollection services)
    {
        services.AddScoped<IDataResponseFactory, DataResponseFactory>();
        return services;
    }
}