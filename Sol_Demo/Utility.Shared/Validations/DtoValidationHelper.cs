using FluentResults;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Utility.Shared.Exceptions;

namespace Utility.Shared.Validations;

public class DtoValidationHelper<TDto, TDtoValidator>
        where TDto : class
        where TDtoValidator : IValidator<TDto>
{
    private readonly IServiceProvider _serviceProvider;

    public DtoValidationHelper(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<Result> ValidateAsync(TDto dto)
    {
        var validator = _serviceProvider.GetRequiredService<TDtoValidator>();
        var validationResult = await validator.ValidateAsync(dto);
        if (validationResult.IsValid == false)
        {
            //return Result.Fail(validationResult.Errors.Select(x => x.ErrorMessage).ToList());
            string errors = string.Join(",", validationResult.Errors.Select(x => x.ErrorMessage));
            return ResultExceptionFactory.Error(errors, HttpStatusCode.BadRequest);
        }

        return Result.Ok();
    }
}