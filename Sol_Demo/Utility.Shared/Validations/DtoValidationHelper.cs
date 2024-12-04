using FluentResults;
using FluentValidation;

namespace Utility.Shared.Validations;

public class DtoValidationHelper<TDto, TDtoValidator>
        where TDto : class
        where TDtoValidator : IValidator<TDto>, new()
{
    public async Task<Result> ValidateAsync(TDto dto)
    {
        var validator = new TDtoValidator();
        var validationResult = await validator.ValidateAsync(dto);
        if (validationResult.IsValid == false)
        {
            return Result.Fail(validationResult.Errors.Select(x => x.ErrorMessage).ToList());
        }

        return Result.Ok();
    }
}