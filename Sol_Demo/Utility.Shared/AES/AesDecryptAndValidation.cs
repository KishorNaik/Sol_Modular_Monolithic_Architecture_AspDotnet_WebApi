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
    private readonly AesHelper _aesHelper = new AesHelper();

    public async Task<DataResponse<TRequestDto>> WrapperAsync<TRequestDto, TDtoValidator>(AesRequestDto aesRequestDto)
        where TRequestDto : class
         where TDtoValidator : IValidator<TRequestDto>, new()
    {
        try
        {
            if (aesRequestDto is null)
                return ResponeException.Error<TRequestDto>("AesRequestDto is null", (int)HttpStatusCode.BadRequest);

            if (aesRequestDto.Body is null)
                return ResponeException.Error<TRequestDto>("Body is null", (int)HttpStatusCode.BadRequest);

            // Decrypt
            String requestBody = aesRequestDto.Body;
            String decryptRequestBodyStr = await _aesHelper.DecryptAsync(requestBody);
            TRequestDto requestDto = JsonConvert.DeserializeObject<TRequestDto>(decryptRequestBodyStr)!;

            // Validate
            Result result = await _dtoValidation.ValidateAsync<TRequestDto, TDtoValidator>(requestDto);

            if (result.IsFailed)
                return ResponeException.Error<TRequestDto>(result.Errors[0].Message, (int)HttpStatusCode.BadRequest);

            return DataResponse.Success<TRequestDto>((int)HttpStatusCode.OK, requestDto, String.Empty);
        }
        catch (Exception ex)
        {
            return ResponeException.Error<TRequestDto>(ex.Message, (int)HttpStatusCode.BadRequest);
        }
    }
}