using FluentResults;
using Models.Shared.Responses;
using Newtonsoft.Json;
using System.Net;
using Utility.Shared.Exceptions;
using Utility.Shared.ServiceHandler;

namespace Utility.Shared.AES;

public class AesEncrypteWrapperParameter<TResponse>
{
    public string? Secret { get; }

    public TResponse? Response { get; }

    public AesEncrypteWrapperParameter(string? secret, TResponse? response)
    {
        Secret = secret;
        Response = response;
    }
}

public interface IAesEncrypteWrapper<TResponseDto> : IServiceHandlerAsync<AesEncrypteWrapperParameter<TResponseDto>, AesResponseDto>
{
}

public class AesEncryptWrapper<TResponseDto> : IAesEncrypteWrapper<TResponseDto>
{
    public async Task<Result<AesResponseDto>> HandleAsync(AesEncrypteWrapperParameter<TResponseDto> @params)
    {
        try
        {
            if (@params is null)
                return ResultExceptionFactory.Error<AesResponseDto>("AesEncrypteWrapperParameter is null", HttpStatusCode.BadRequest);

            if (@params.Response is null)
                return ResultExceptionFactory.Error<AesResponseDto>("Response is null", HttpStatusCode.BadRequest);

            if (@params.Secret is null)
                return ResultExceptionFactory.Error<AesResponseDto>("Secret is null", HttpStatusCode.BadRequest);

            AesHelper aesHelper = new AesHelper(@params.Secret!);
            string body = JsonConvert.SerializeObject(@params.Response! as object);
            string encryptRequestBodyStr = await aesHelper.EncryptAsync(body);

            AesResponseDto aesResponseDto = new AesResponseDto();
            aesResponseDto.Body = encryptRequestBodyStr;
            return Result.Ok(aesResponseDto);
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error<AesResponseDto>(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}