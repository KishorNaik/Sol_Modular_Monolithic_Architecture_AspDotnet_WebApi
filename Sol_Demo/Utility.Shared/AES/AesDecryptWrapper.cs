using FluentResults;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Models.Shared.Requests;
using Models.Shared.Responses;
using Newtonsoft.Json;
using System.Net;
using Utility.Shared.Exceptions;
using Utility.Shared.Response;
using Utility.Shared.ServiceHandler;
using Utility.Shared.Validations;

namespace Utility.Shared.AES;

public class AesDecrypteWrapperParameter
{
    public AesRequestDto? AesRequest { get; }

    public string? Secret { get; }

    public AesDecrypteWrapperParameter(AesRequestDto aesRequest, string? secret)
    {
        AesRequest = aesRequest;
        Secret = secret;
    }
}

public interface IAesDecrypteWrapper<TRequestDto> : IServiceHandlerAsync<AesDecrypteWrapperParameter, TRequestDto>
{
}

public class AesDecrypteWrapper<TRequestDto> : IAesDecrypteWrapper<TRequestDto>
{
    public async Task<Result<TRequestDto>> HandleAsync(AesDecrypteWrapperParameter @params)
    {
        try
        {
            if (@params is null)
                return ResultExceptionFactory.Error<TRequestDto>("AesDecrypteWrapperParameter is null", HttpStatusCode.BadRequest);

            if (@params.AesRequest is null)
                return ResultExceptionFactory.Error<TRequestDto>("AesRequest is null", HttpStatusCode.BadRequest);

            if (@params.AesRequest.Body is null)
                return ResultExceptionFactory.Error<TRequestDto>("Body is null", HttpStatusCode.BadRequest);

            if (@params.Secret is null)
                return ResultExceptionFactory.Error<TRequestDto>("Secret is null", HttpStatusCode.BadRequest);

            AesRequestDto aesRequestDto = @params.AesRequest;

            AesHelper aesHelper = new AesHelper(@params.Secret!);

            String requestBody = aesRequestDto.Body;
            String decryptRequestBodyStr = await aesHelper.DecryptAsync(requestBody);
            TRequestDto requestDto = JsonConvert.DeserializeObject<TRequestDto>(decryptRequestBodyStr)!;

            if (requestDto is null)
                return ResultExceptionFactory.Error<TRequestDto>("RequestDto is null", HttpStatusCode.BadRequest);

            return Result.Ok(requestDto);
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error<TRequestDto>(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}

//public class AesDecryptAndValidation
//{
//    private readonly DtoValidation _dtoValidation = new DtoValidation();
//    private readonly AesHelper _aesHelper;

//    public AesDecryptAndValidation(string secretKey) => _aesHelper = new AesHelper(secretKey);

//    public async Task<Result<TRequestDto>> WrapperAsync<TRequestDto, TDtoValidator>(AesRequestDto aesRequestDto)
//        where TRequestDto : class
//        where TDtoValidator : IValidator<TRequestDto>, new()
//    {
//        try
//        {
//            if (aesRequestDto is null)
//                return ResultExceptionFactory.Error<TRequestDto>("AesRequestDto is null", HttpStatusCode.BadRequest);

//            if (aesRequestDto.Body is null)
//                return ResultExceptionFactory.Error<TRequestDto>("Body is null", HttpStatusCode.BadRequest);

//            // Decrypt
//            String requestBody = aesRequestDto.Body;
//            String decryptRequestBodyStr = await _aesHelper.DecryptAsync(requestBody);
//            TRequestDto requestDto = JsonConvert.DeserializeObject<TRequestDto>(decryptRequestBodyStr)!;

//            // Validate
//            Result result = await _dtoValidation.ValidateAsync<TRequestDto, TDtoValidator>(requestDto);

//            if (result.IsFailed)
//                return ResultExceptionFactory.Error<TRequestDto>(result.Errors[0]);

//            return Result.Ok(requestDto);
//        }
//        catch (Exception ex)
//        {
//            return ResultExceptionFactory.Error<TRequestDto>(ex.Message, HttpStatusCode.InternalServerError);
//        }
//    }
//}