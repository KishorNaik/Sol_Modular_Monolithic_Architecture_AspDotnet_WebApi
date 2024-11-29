using FluentResults;
using FluentValidation;
using Models.Shared.Requests;
using Models.Shared.Responses;
using Newtonsoft.Json;
using System.Net;
using Utility.Shared.Exceptions;
using Utility.Shared.Response;
using Utility.Shared.Validations;

namespace Utility.Shared.AES;

public class AesDecryptAndValidation
{
    private readonly DtoValidation _dtoValidation = new DtoValidation();
    private readonly AesHelper _aesHelper;

    public AesDecryptAndValidation(string secretKey) => _aesHelper = new AesHelper(secretKey);

    public async Task<Result<TRequestDto>> WrapperAsync<TRequestDto, TDtoValidator>(AesRequestDto aesRequestDto)
        where TRequestDto : class
        where TDtoValidator : IValidator<TRequestDto>, new()
    {
        try
        {
            if (aesRequestDto is null)
                return ResultExceptionFactory.Error<TRequestDto>("AesRequestDto is null", HttpStatusCode.BadRequest);

            if (aesRequestDto.Body is null)
                return ResultExceptionFactory.Error<TRequestDto>("Body is null", HttpStatusCode.BadRequest);

            // Decrypt
            String requestBody = aesRequestDto.Body;
            String decryptRequestBodyStr = await _aesHelper.DecryptAsync(requestBody);
            TRequestDto requestDto = JsonConvert.DeserializeObject<TRequestDto>(decryptRequestBodyStr)!;

            // Validate
            Result result = await _dtoValidation.ValidateAsync<TRequestDto, TDtoValidator>(requestDto);

            if (result.IsFailed)
                return ResultExceptionFactory.Error<TRequestDto>(result.Errors[0]);

            return Result.Ok(requestDto);
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error<TRequestDto>(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}