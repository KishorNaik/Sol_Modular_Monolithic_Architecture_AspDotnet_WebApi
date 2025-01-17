﻿using FluentResults;
using FluentValidation;
using Hangfire;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Models.Shared.Constant;
using Models.Shared.Requests;
using Newtonsoft.Json;
using Organization.Applications.Features.v1.AddOrganization;
using Organization.Applications.Shared.Cache;
using Organization.Contracts.Features.AddOrganizations;
using Organization.Contracts.Features.GetOrganizationByIdentifier;
using Organization.Infrastructures.Entities;
using Organization.Infrastructures.Services.GetOrganizationByIdentifer;
using Organization.Infrastructures.Services.GetVersionByIdentifier;
using System.Reflection.Metadata.Ecma335;
using User.Shared.Services.HmacSignature;
using Utility.Shared.AES;
using Utility.Shared.Cache;
using Utility.Shared.Config;
using Utility.Shared.Exceptions;
using Utility.Shared.Response;
using Utility.Shared.ServiceHandler;
using Utility.Shared.Validations;

namespace Organization.Applications.Features.v1.GetOrganizationByIdentifier;

#region Controller

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/organizations")]
[Tags("Organizations")]
public class GetOrganizationByIdentifierController : OrganizationBaseController
{

    private readonly ILogger<GetOrganizationByIdentifierController> _logger = null;

    public GetOrganizationByIdentifierController(IMediator mediator, ILogger<GetOrganizationByIdentifierController> logger) : base(mediator)
    {
        _logger = logger;
    }

    [HttpGet("{identifier}")]
    [MapToApiVersion(1)]
    [DisableRateLimiting]
    //[AllowAnonymous]
    [Authorize(Policy =ConstantValue.UserOnlyPolicy)]
    [HmacSignatureValidationService]
    [ProducesResponseType<DataResponse<AesResponseDto>>((int)HttpStatusCode.OK)]
    [ProducesResponseType<DataResponse<AesResponseDto>>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<DataResponse<AesResponseDto>>((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> GetAsync([FromRoute] GetOrganizationByIdentifierRequestDto request, CancellationToken cancellationToken = default)
    {

        _logger.LogInformation("Get Organization By Identifier");

        var response = await this.Mediator.Send(new GetOrganizationByIdentifierQuery(request), cancellationToken);
        return base.StatusCode(Convert.ToInt32(response.StatusCode), response);
    }
}

#endregion Controller

#region Validation Service

[ScopedService]
public sealed class GetOrganizationByIdentifierValidator : AbstractValidator<GetOrganizationByIdentifierRequestDto>
{
    private readonly IActionContextAccessor actionContextAccessor;

    public GetOrganizationByIdentifierValidator(IActionContextAccessor actionContextAccessor)
    {
        this.actionContextAccessor = actionContextAccessor;

        this.IdentifierValidation();
    }

    private void IdentifierValidation()
    {
        RuleFor(x => x.Identifier)
             .Must((context, id, propertyValidatorContext) =>
             {
                 var identifer = (string)actionContextAccessor.ActionContext.RouteData.Values.GetValueOrDefault("identifier") ?? id?.ToString();

                 if (identifer is null)
                 {
                     return false;
                 }

                 return true;
             })
             .WithMessage("Identifier is required")
             .WithErrorCode("Identifier")
             .Must((context, id, propertyValidatorContext) =>
             {
                 var identifer = (string)actionContextAccessor.ActionContext.RouteData.Values.GetValueOrDefault("identifier") ?? id?.ToString();

                 Guid identifierGuid;
                 var flag = Guid.TryParse(identifer, out identifierGuid);

                 return flag;
             })
             .WithMessage("Identifier is not valid")
             .WithErrorCode("Identifier");
    }
}

public interface IGetOrganizationByIdentifierValidationService : IServiceHandlerVoidAsync<GetOrganizationByIdentifierRequestDto>
{
}

[ScopedService(typeof(IGetOrganizationByIdentifierValidationService))]
public sealed class GetOrganizationByIdentifierValidationService : IGetOrganizationByIdentifierValidationService
{
    private readonly IServiceProvider _serviceProvider = null;

    public GetOrganizationByIdentifierValidationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    async Task<Result> IServiceHandlerVoidAsync<GetOrganizationByIdentifierRequestDto>.HandleAsync(GetOrganizationByIdentifierRequestDto @params)
    {
        try
        {
            if (@params is null)
                return ResultExceptionFactory.Error($"{nameof(GetOrganizationByIdentifierRequestDto)} object is null", HttpStatusCode.BadRequest);

            // Validation
            var dtoValidationHelper = new DtoValidationHelper<GetOrganizationByIdentifierRequestDto, GetOrganizationByIdentifierValidator>(_serviceProvider);

            var validationResult = await dtoValidationHelper.ValidateAsync(@params);

            if (validationResult.IsFailed)
                return ResultExceptionFactory.Error(validationResult.Errors[0].Message, HttpStatusCode.BadRequest);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}

#endregion Validation Service


#region Response Service

public class GetOrganizationByIdentifierResponseServiceParameter
{
    public Torganization? Torganization { get; }

    public GetOrganizationByIdentifierResponseServiceParameter(Torganization? torganization)
    {
        Torganization = torganization;
    }
}

public interface IGetOrganizationByIdentifierResponseService : IServiceHandlerAsync<GetOrganizationByIdentifierResponseServiceParameter, AesResponseDto>
{
}

[ScopedService(typeof(IGetOrganizationByIdentifierResponseService))]
public sealed class GetOrganizationByIdentifierResponseService : IGetOrganizationByIdentifierResponseService
{
    private readonly IConfigHelper _configHelper;

    public GetOrganizationByIdentifierResponseService(IConfigHelper configHelper)
    {
        _configHelper = configHelper;
    }

    async Task<Result<AesResponseDto>> IServiceHandlerAsync<GetOrganizationByIdentifierResponseServiceParameter, AesResponseDto>.HandleAsync(GetOrganizationByIdentifierResponseServiceParameter @params)
    {
        try
        {
            if (@params is null)
                return ResultExceptionFactory.Error($"{nameof(GetOrganizationByIdentifierResponseServiceParameter)} object is null", HttpStatusCode.BadRequest);

            if (@params?.Torganization is null)
                return ResultExceptionFactory.Error($"{nameof(Torganization)} object is null", HttpStatusCode.BadRequest);

            // Get Aes Secret Value from Config Manager
            var aesSecret = _configHelper.GetValue(ConstantValue.AesSecretKey!);
            if (aesSecret.IsFailed)
                return ResultExceptionFactory.Error<AesResponseDto>(aesSecret.Errors[0]);

            Torganization torganization = @params.Torganization;

            // Map
            GetOrganizationByIdentifierResponseDto getOrganizationByIdentifierResponseDto = new GetOrganizationByIdentifierResponseDto
            {
                Identifier = torganization.Identifier,
                Name = torganization.Name
            };

            // Encrypt Response
            IAesEncrypteWrapper<GetOrganizationByIdentifierResponseDto> aesEncrypteWrapper =
            new AesEncryptWrapper<GetOrganizationByIdentifierResponseDto>();

            var aesEncryptionResult = await aesEncrypteWrapper.HandleAsync(new AesEncrypteWrapperParameter<GetOrganizationByIdentifierResponseDto>(aesSecret.Value, getOrganizationByIdentifierResponseDto));
            if (aesEncryptionResult.IsFailed)
                return ResultExceptionFactory.Error<AesResponseDto>(aesEncryptionResult.Errors[0]);

            AesResponseDto aesResponseDto = new AesResponseDto
            {
                Body = aesEncryptionResult.Value
            };

            return Result.Ok(aesResponseDto);
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error(ex.Message, httpStatusCode: HttpStatusCode.InternalServerError);
        }
    }
}

#endregion Response Service

#region Query Handler

public class GetOrganizationByIdentifierQuery : IRequest<DataResponse<AesResponseDto>>
{
    public GetOrganizationByIdentifierRequestDto? Request { get; }

    public GetOrganizationByIdentifierQuery(GetOrganizationByIdentifierRequestDto? request)
    {
        Request = request;
    }
}

public sealed class GetOrganizationByIdentifierQueryHandler : IRequestHandler<GetOrganizationByIdentifierQuery, DataResponse<AesResponseDto>>
{
    private readonly IDataResponseFactory _dataResponseFactory = null;
    private readonly IGetOrganizationByIdentifierValidationService _getOrganizationByIdentifierValidationService = null;
    private readonly IOrganizationSharedCacheService _organizationSharedCacheService = null;
    private readonly IGetOrganizationByIdentifierResponseService _getOrganizationByIdentifierResponseService = null;

    public GetOrganizationByIdentifierQueryHandler(
        IDataResponseFactory dataResponseFactory,
        IGetOrganizationByIdentifierValidationService getOrganizationByIdentifierValidationService,
        IOrganizationSharedCacheService organizationSharedCacheService,
        IGetOrganizationByIdentifierResponseService getOrganizationByIdentifierResponseService
        )
    {
        _dataResponseFactory = dataResponseFactory;
        _getOrganizationByIdentifierValidationService = getOrganizationByIdentifierValidationService;
        _organizationSharedCacheService = organizationSharedCacheService;
        _getOrganizationByIdentifierResponseService = getOrganizationByIdentifierResponseService;
    }

    async Task<DataResponse<AesResponseDto>> IRequestHandler<GetOrganizationByIdentifierQuery, DataResponse<AesResponseDto>>.Handle(GetOrganizationByIdentifierQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (request is null)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>("Request object is null", (int)HttpStatusCode.BadRequest);

            GetOrganizationByIdentifierRequestDto requestDto = request.Request!;

            // Validation
            var validationResult = await _getOrganizationByIdentifierValidationService.HandleAsync(requestDto);
            if (validationResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(validationResult.Errors[0].Message, (int)validationResult.Errors[0].Metadata[ConstantValue.StatusCode]);

            Guid? identifer = request.Request.Identifier;

            // Get Data
            var getDataResult = await _organizationSharedCacheService.HandleAsync(new OrganizationSharedCacheServiceParameter(identifer, cancellationToken));
            if (getDataResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(getDataResult.Errors[0].Message, (int)getDataResult.Errors[0].Metadata[ConstantValue.StatusCode]);

            Torganization torganization = getDataResult.Value.Torganization!;

            // Response
            var responseResult = await _getOrganizationByIdentifierResponseService.HandleAsync(new GetOrganizationByIdentifierResponseServiceParameter(torganization));
            if (responseResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(responseResult.Errors[0].Message, (int)responseResult.Errors[0].Metadata[ConstantValue.StatusCode]);

            return await _dataResponseFactory.SuccessAsync<AesResponseDto>((int)HttpStatusCode.OK, responseResult.Value!, "Org Data Retrieved Successfully");
        }
        catch (Exception ex)
        {
            return await _dataResponseFactory.ErrorAsync<AesResponseDto>(ex.Message, (int)HttpStatusCode.InternalServerError);
        }
    }
}

#endregion Query Handler

#region Decrypt Service

public class GetOrganizationByIdentifierDecryptServiceParameter
{
    public AesResponseDto? Response { get; }

    public GetOrganizationByIdentifierDecryptServiceParameter(AesResponseDto? response)
    {
        Response = response;
    }
}

public class GetOrganizationByIdentifierDecryptServiceResult
{
    public GetOrganizationByIdentifierResponseDto Response { get; }

    public GetOrganizationByIdentifierDecryptServiceResult(GetOrganizationByIdentifierResponseDto response)
    {
        Response = response;
    }
}

public interface IGetOrganizationByIdentifierDecryptService : IServiceHandlerAsync<GetOrganizationByIdentifierDecryptServiceParameter, GetOrganizationByIdentifierDecryptServiceResult>
{
}

[ScopedService(typeof(IGetOrganizationByIdentifierDecryptService))]
public sealed class GetOrganizationByIdentifierDecryptService : IGetOrganizationByIdentifierDecryptService
{
    private readonly IConfigHelper _configHelper = null;

    public GetOrganizationByIdentifierDecryptService(IConfigHelper configHelper)
    {
        _configHelper = configHelper;
    }

    async Task<Result<GetOrganizationByIdentifierDecryptServiceResult>> IServiceHandlerAsync<GetOrganizationByIdentifierDecryptServiceParameter, GetOrganizationByIdentifierDecryptServiceResult>.HandleAsync(GetOrganizationByIdentifierDecryptServiceParameter @params)
    {
        try
        {
            if (@params is null)
                return ResultExceptionFactory.Error($"{nameof(GetOrganizationByIdentifierDecryptServiceParameter)} object is null", HttpStatusCode.BadRequest);

            if (@params.Response is null)
                return ResultExceptionFactory.Error($"{nameof(AesResponseDto)} object is null", HttpStatusCode.BadRequest);

            AesResponseDto response = @params.Response!;

            // Get Aes Secret Value from Config Manager
            var aesSecret = _configHelper.GetValue(ConstantValue.AesSecretKey!);
            if (aesSecret.IsFailed)
                return ResultExceptionFactory.Error<GetOrganizationByIdentifierDecryptServiceResult>(aesSecret.Errors[0]);

            var aesSecretValue = aesSecret.Value!;

            // Decrypt Response
            IAesDecrypteWrapper<GetOrganizationByIdentifierResponseDto> aesDecrypteWrapper =
                new AesDecrypteWrapper<GetOrganizationByIdentifierResponseDto>();

            AesDecrypteWrapperParameter aesDecrypteWrapperParameter =
                new AesDecrypteWrapperParameter(response.Body!, aesSecret.Value);

            var aesDecryptionResult = await aesDecrypteWrapper.HandleAsync(aesDecrypteWrapperParameter);
            if (aesDecryptionResult.IsFailed)
                return ResultExceptionFactory.Error<GetOrganizationByIdentifierDecryptServiceResult>(aesDecryptionResult.Errors[0]);

            GetOrganizationByIdentifierResponseDto getOrganizationByIdentifierResponseDto = aesDecryptionResult.Value!;

            // Map
            GetOrganizationByIdentifierDecryptServiceResult getOrganizationByIdentifierDecryptServiceResult =
                new GetOrganizationByIdentifierDecryptServiceResult(getOrganizationByIdentifierResponseDto);

            return Result.Ok(getOrganizationByIdentifierDecryptServiceResult);
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}

#endregion Decrypt Service

#region Query Integration Event Service Handler

public sealed class GetOrganizationByIdentifierQueryIntegrationEventServiceHandler : IRequestHandler<GetOrganizationByIdentifierIntegrationEventService, DataResponse<GetOrganizationByIdentifierResponseDto>>
{
    private readonly IMediator _mediator = null;
    private readonly IDataResponseFactory _dataResponseFactory = null;
    private readonly IGetOrganizationByIdentifierDecryptService _getOrganizationByIdentifierDecryptService = null;

    public GetOrganizationByIdentifierQueryIntegrationEventServiceHandler(
        IMediator mediator,
        IDataResponseFactory dataResponseFactory,
        IGetOrganizationByIdentifierDecryptService getOrganizationByIdentifierDecryptService)
    {
        _mediator = mediator;
        _dataResponseFactory = dataResponseFactory;
        _getOrganizationByIdentifierDecryptService = getOrganizationByIdentifierDecryptService;
    }

    async Task<DataResponse<GetOrganizationByIdentifierResponseDto>> IRequestHandler<GetOrganizationByIdentifierIntegrationEventService, DataResponse<GetOrganizationByIdentifierResponseDto>>
        .Handle(GetOrganizationByIdentifierIntegrationEventService request, CancellationToken cancellationToken)
    {
        try
        {
            if (request is null)
                return await _dataResponseFactory.ErrorAsync<GetOrganizationByIdentifierResponseDto>("Request object is null", (int)HttpStatusCode.BadRequest);

            GetOrganizationByIdentifierRequestDto requestDto = request.Request!;

            // Query
            var queryResult = await _mediator.Send(new GetOrganizationByIdentifierQuery(requestDto));

            if (queryResult.Success == false)
                return await _dataResponseFactory.ErrorAsync<GetOrganizationByIdentifierResponseDto>(queryResult.Message!, (int)queryResult.StatusCode!);

            // Decrypt
            var decryptResult = await _getOrganizationByIdentifierDecryptService.HandleAsync(new GetOrganizationByIdentifierDecryptServiceParameter(queryResult.Data));
            if (decryptResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<GetOrganizationByIdentifierResponseDto>(decryptResult.Errors[0].Message, (int)decryptResult.Errors[0].Metadata[ConstantValue.StatusCode]);

            GetOrganizationByIdentifierResponseDto getOrganizationByIdentifierResponseDto = decryptResult.Value.Response!;

            return await _dataResponseFactory.SuccessAsync<GetOrganizationByIdentifierResponseDto>(queryResult.StatusCode!, getOrganizationByIdentifierResponseDto, queryResult.Message!);
        }
        catch (Exception ex)
        {
            return await _dataResponseFactory.ErrorAsync<GetOrganizationByIdentifierResponseDto>(ex.Message, (int)HttpStatusCode.InternalServerError);
        }
    }
}

#endregion Query Integration Event Service Handler