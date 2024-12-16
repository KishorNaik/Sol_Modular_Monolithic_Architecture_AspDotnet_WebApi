
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Users.Infrastructures.Services.GetUsersByIdentifier;
using Utility.Shared.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Utility.Shared.Traces;
using Microsoft.Extensions.Options;
using Models.Shared.Responses;
using System.Diagnostics;
using System.Text.Json;
using User.Contracts.Shared.Service.HmacSignature;
using Utility.Shared.ServiceHandler;
using Azure.Core;
using System.Threading;
using sorovi.DependencyInjection.AutoRegister;

namespace User.Shared.Services.HmacSignature;

#region Service


[ScopedService(typeof(IGetHmacSecretKeyService))]
public class GetHmacSecretKeyService : IGetHmacSecretKeyService
{
    private readonly IDistributedCache _distributedCache;

    public GetHmacSecretKeyService(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

  
    async Task<Result<string>> IServiceHandlerAsync<GetHmacSecretKeyServiceParameters, string>.HandleAsync(GetHmacSecretKeyServiceParameters @params)
    {
        try
        {
            if (@params is null)
                return ResultExceptionFactory.Error<string>($"{nameof(GetHmacSecretKeyServiceParameters)} is null", HttpStatusCode.BadRequest);

            if (@params.ClientId is null)
                return ResultExceptionFactory.Error<string>($"{nameof(@params.ClientId)} is null", HttpStatusCode.BadRequest);

            // Set Cache Name
            string cacheClientName = $"UserClient-{@params.ClientId}";

            // Get Cache Value
            string? cacheValue = await _distributedCache.GetStringAsync(cacheClientName, @params.CancellationToken);

            if (cacheValue is null)
                return ResultExceptionFactory.Error<string>($"Unauthorized access", HttpStatusCode.Unauthorized);

            // Get Cache Value
            GetUserByIdentiferDbServiceResult cacheValueResult = JsonConvert.DeserializeObject<GetUserByIdentiferDbServiceResult>(cacheValue)!;

            if (cacheValueResult is null)
                return ResultExceptionFactory.Error<string>($"Unauthorized access", HttpStatusCode.Unauthorized);

            // Get HMac Secret Key
            string? hmacSecretKey = cacheValueResult!.UserCredentials!.HmacSecretKey;

            if (hmacSecretKey is null)
                return ResultExceptionFactory.Error<string>($"Unauthorized access", HttpStatusCode.Unauthorized);

            return Result.Ok(hmacSecretKey);

        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}
#endregion

#region Filters

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class HmacSignatureValidationServiceAttribute : Attribute, IAsyncActionFilter
{

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {

        var serviceProvider = context.HttpContext.RequestServices;
        var getHmacSecretService = serviceProvider.GetRequiredService<IGetHmacSecretKeyService>();
        var traceIdService = serviceProvider.GetRequiredService<ITraceIdService>();

        Result<string> traceIdResult = null;
        try
        {

            // Get Trace Id
            traceIdResult = await GetTraceIdAsync(context.HttpContext, traceIdService);
            if (traceIdResult.IsFailed)
                throw new Exception(traceIdResult.Errors[0].Message);

            // Read the request body
            // Clone the request body
            context.HttpContext.Request.EnableBuffering();

            context.HttpContext.Request.Body.Position = 0;
            //using var reader = new StreamReader(context.HttpContext.Request.Body);
            using var reader = new StreamReader(context.HttpContext.Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.HttpContext.Request.Body.Position = 0;

            // string to byte array
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);

            // Get the client ID from the headers
            var clientId = context.HttpContext.Request.Headers["X-CLIENT-ID"].ToString();

            // Fetch the secret key from the database using the client ID
            var secretResult = await GetSecretKeyFromDatabaseAsync(clientId,getHmacSecretService);
            if(secretResult.IsFailed)
                throw new Exception(secretResult.Errors[0].Message);

            // Get the signature from the headers
            var clientSignature = context.HttpContext.Request.Headers["X-AUTH-SIGNATURE"].ToString();
            if(clientSignature is null)
                throw new Exception("Signature not found");

            // Generate the server-side signature
            var serverSignature = GenerateSignature(bodyBytes, secretResult.Value!);
            if(serverSignature is null)
                throw new Exception("Signature generation failed");

            // Compare signatures
            if (clientSignature == serverSignature)
            {
                // Process the order
                await next();
            }
            else
            {
                //context.Result = new UnauthorizedResult();
                await HandleException(context.HttpContext,"Unauthorized access", traceIdResult.Value!);
            }
        }
        catch(Exception ex)
        {
            //context.Result = new UnauthorizedResult();
            await HandleException(context.HttpContext,ex.Message, traceIdResult!.Value!);
        }

       
    }

    private async Task<Result<string>> GetSecretKeyFromDatabaseAsync(string clientId, IGetHmacSecretKeyService getHmacSecretKeyService,CancellationToken cancellationToken=default)
    {
        var result = await getHmacSecretKeyService.HandleAsync(new GetHmacSecretKeyServiceParameters(clientId, cancellationToken));
        if (result.IsFailed)
            return ResultExceptionFactory.Error<string>(result.Errors[0]);

        return Result.Ok(result.Value);
    }

    private string GenerateSignature(byte[] payload, string secret)
    {
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            byte[] hash = hmac.ComputeHash(payload);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }

    private async Task<Result<string>> GetTraceIdAsync(HttpContext httpContext, ITraceIdService traceIdService)
    {
        var traceId=await traceIdService.GetOrGenerateTraceId(httpContext);
        return Result.Ok(traceId);
        
    }

    private async Task HandleException(HttpContext httpContext,string message, string traceId)
    {
        httpContext.Response.ContentType = "application/json";

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null
        };
        var errorHandler = new ErrorHandlerModel(false,401,message, traceId);
        await httpContext.Response.WriteAsJsonAsync(errorHandler, options);
    }
}
#endregion