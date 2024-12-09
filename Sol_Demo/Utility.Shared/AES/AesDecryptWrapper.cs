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

public class AesDecrypteWrapperParameter<TParams>
{
    public TParams? Params { get; }

    public string? Secret { get; }

    public AesDecrypteWrapperParameter(TParams @params, string? secret)
    {
        Params = @params;
        Secret = secret;
    }
}

public interface IAesDecrypteWrapper<TParams, TResult> : IServiceHandlerAsync<AesDecrypteWrapperParameter<TParams>, TResult>
{
}

public class AesDecrypteWrapper<TParams, TResult> : IAesDecrypteWrapper<TParams, TResult>
{
    //public async Task<Result<TRequestDto>> HandleAsync(AesDecrypteWrapperParameter @params)
    //{
    //    try
    //    {
    //        if (@params is null)
    //            return ResultExceptionFactory.Error<TRequestDto>("AesDecrypteWrapperParameter is null", HttpStatusCode.BadRequest);

    //        if (@params.AesRequest is null)
    //            return ResultExceptionFactory.Error<TRequestDto>("AesRequest is null", HttpStatusCode.BadRequest);

    //        if (@params.AesRequest.Body is null)
    //            return ResultExceptionFactory.Error<TRequestDto>("Body is null", HttpStatusCode.BadRequest);

    //        if (@params.Secret is null)
    //            return ResultExceptionFactory.Error<TRequestDto>("Secret is null", HttpStatusCode.BadRequest);

    //        AesRequestDto aesRequestDto = @params.AesRequest;

    //        AesHelper aesHelper = new AesHelper(@params.Secret!);

    //        String requestBody = aesRequestDto.Body;
    //        String decryptRequestBodyStr = await aesHelper.DecryptAsync(requestBody);
    //        TRequestDto requestDto = JsonConvert.DeserializeObject<TRequestDto>(decryptRequestBodyStr)!;

    //        if (requestDto is null)
    //            return ResultExceptionFactory.Error<TRequestDto>("RequestDto is null", HttpStatusCode.BadRequest);

    //        return Result.Ok(requestDto);
    //    }
    //    catch (Exception ex)
    //    {
    //        return ResultExceptionFactory.Error<TRequestDto>(ex.Message, HttpStatusCode.InternalServerError);
    //    }
    //}
    async Task<Result<TResult>> IServiceHandlerAsync<AesDecrypteWrapperParameter<TParams>, TResult>.HandleAsync(AesDecrypteWrapperParameter<TParams> @params)
    {
        try
        {
            if (@params is null)
                return ResultExceptionFactory.Error<TResult>("AesDecrypteWrapperParameter is null", HttpStatusCode.BadRequest);

            if (@params.Params is null)
                return ResultExceptionFactory.Error<TResult>("Params is null", HttpStatusCode.BadRequest);

            if (@params.Secret is null)
                return ResultExceptionFactory.Error<TResult>("Secret is null", HttpStatusCode.BadRequest);

            AesHelper aesHelper = new AesHelper(@params.Secret!);

            String requestBody = JsonConvert.SerializeObject(@params.Params as object);

            if (requestBody is null)
                return ResultExceptionFactory.Error<TResult>("RequestBody is null", HttpStatusCode.BadRequest);

            String decryptRequestBodyStr = await aesHelper.DecryptAsync(requestBody);

            if (decryptRequestBodyStr is null)
                return ResultExceptionFactory.Error<TResult>("DecryptRequestBodyStr is null", HttpStatusCode.BadRequest);

            TResult result = JsonConvert.DeserializeObject<TResult>(decryptRequestBodyStr)!;

            if (result is null)
                return ResultExceptionFactory.Error<TResult>($"{typeof(TResult)} is null", HttpStatusCode.BadRequest);

            return Result.Ok(result);
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error<TResult>(ex.Message, HttpStatusCode.InternalServerError);
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