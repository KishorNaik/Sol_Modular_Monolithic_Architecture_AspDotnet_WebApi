using FluentResults;
using Models.Shared.Responses;
using Newtonsoft.Json;
using System.Net;
using Utility.Shared.Exceptions;
using Utility.Shared.ServiceHandler;

namespace Utility.Shared.AES;

public class AesEncrypteWrapperParameter<TParams>
{
    public string? Secret { get; }

    public TParams? Params { get; }

    public AesEncrypteWrapperParameter(string? secret, TParams? @params)
    {
        Secret = secret;
        Params = @params;
    }
}

public interface IAesEncrypteWrapper<TParams> : IServiceHandlerAsync<AesEncrypteWrapperParameter<TParams>, string>
{
}

public class AesEncryptWrapper<TParams> : IAesEncrypteWrapper<TParams>
{
    //public async Task<Result<AesResponseDto>> HandleAsync(AesEncrypteWrapperParameter<TResponseDto> @params)
    //{
    //    try
    //    {
    //        if (@params is null)
    //            return ResultExceptionFactory.Error<AesResponseDto>("AesEncrypteWrapperParameter is null", HttpStatusCode.BadRequest);

    //        if (@params.Response is null)
    //            return ResultExceptionFactory.Error<AesResponseDto>("Response is null", HttpStatusCode.BadRequest);

    //        if (@params.Secret is null)
    //            return ResultExceptionFactory.Error<AesResponseDto>("Secret is null", HttpStatusCode.BadRequest);

    //        AesHelper aesHelper = new AesHelper(@params.Secret!);
    //        string body = JsonConvert.SerializeObject(@params.Response! as object);
    //        string encryptRequestBodyStr = await aesHelper.EncryptAsync(body);

    //        AesResponseDto aesResponseDto = new AesResponseDto();
    //        aesResponseDto.Body = encryptRequestBodyStr;
    //        return Result.Ok(aesResponseDto);
    //    }
    //    catch (Exception ex)
    //    {
    //        return ResultExceptionFactory.Error<AesResponseDto>(ex.Message, HttpStatusCode.InternalServerError);
    //    }
    //}

    async Task<Result<string>> IServiceHandlerAsync<AesEncrypteWrapperParameter<TParams>, string>.HandleAsync(AesEncrypteWrapperParameter<TParams> @params)
    {
        try
        {
            if (@params is null)
                return ResultExceptionFactory.Error<string>("@params is null", HttpStatusCode.BadRequest);

            if (@params.Params is null)
                return ResultExceptionFactory.Error<string>("params is null", HttpStatusCode.BadRequest);

            if (@params.Secret is null)
                return ResultExceptionFactory.Error<string>("Secret is null", HttpStatusCode.BadRequest);

            AesHelper aesHelper = new AesHelper(@params.Secret!);
            string body = JsonConvert.SerializeObject(@params.Params! as object);

            if (body is null)
                return ResultExceptionFactory.Error<string>("Body is null", HttpStatusCode.BadRequest);

            string encryptRequestBodyStr = await aesHelper.EncryptAsync(body);

            if (encryptRequestBodyStr is null)
                return ResultExceptionFactory.Error<string>("EncryptRequestBodyStr is null", HttpStatusCode.BadRequest);

            return Result.Ok(encryptRequestBodyStr);
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error<string>(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}