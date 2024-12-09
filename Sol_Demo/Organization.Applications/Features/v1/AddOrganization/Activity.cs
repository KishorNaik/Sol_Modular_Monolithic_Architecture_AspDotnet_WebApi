using FluentResults;
using FluentValidation;
using Hangfire;
using Microsoft.Extensions.Logging;
using Models.Shared.Constant;
using Models.Shared.Enums;
using Models.Shared.Requests;
using NetTopologySuite.Utilities;
using Newtonsoft.Json;
using Organization.Applications.Shared.Cache;
using Organization.Contracts.Features.AddOrganizations;
using Organization.Infrastructures.Entities;
using Organization.Infrastructures.Services.AddOrganization;
using sorovi.DependencyInjection.AutoRegister;
using System.Text.RegularExpressions;
using Utility.Shared.AES;
using Utility.Shared.Config;
using Utility.Shared.Exceptions;
using Utility.Shared.Response;
using Utility.Shared.ServiceHandler;
using Utility.Shared.Validations;

namespace Organization.Applications.Features.v1.AddOrganization;

#region Controller

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/organizations")]
[Tags("Organizations")]
public class AddOrganizationController : OrganizationBaseController
{
    public AddOrganizationController(
        IMediator mediator)
    : base(mediator)
    {
    }

    [HttpPost("create")]
    [MapToApiVersion(1)]
    [DisableRateLimiting]
    [AllowAnonymous]
    [ProducesResponseType<DataResponse<AddOrganizationResponseDto>>((int)HttpStatusCode.Created)]
    [ProducesResponseType<DataResponse<AddOrganizationResponseDto>>((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> CreateAsync([FromBody] AesRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = await this.Mediator.Send(new AddOrganizationCommand(request), cancellationToken);
        return base.StatusCode(Convert.ToInt32(response.StatusCode), response);
    }
}

#endregion Controller

#region Validation Service

public class AddOrganizationValidator : AbstractValidator<AddOrganizationRequestDto>
{
    public AddOrganizationValidator()
    {
        this.NameValidation();
    }

    private void NameValidation()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode("Name")
            .Length(0, 100).WithMessage("Name must be less than 100 characters.").WithErrorCode("Name")
            .Matches(new Regex(@"^[a-zA-Z0-9 ]*$")).WithMessage("Name must not contain special characters.").WithErrorCode("Name")
            .Must(name => !Regex.IsMatch(name, "<.*>|<.*|.*>")).WithMessage("Name must not contain HTML tags.").WithErrorCode("Name")
            .Must(name => !Regex.IsMatch(name, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>")).WithMessage("Name must not contain JavaScript.").WithErrorCode("Name");
    }
}

public interface IAddOrgnizationValidationService : IServiceHandlerVoidAsync<AddOrganizationRequestDto>
{
}

[ScopedService(typeof(IAddOrgnizationValidationService))]
public class AddOrgnizationValidationService : IAddOrgnizationValidationService
{
    async Task<Result> IServiceHandlerVoidAsync<AddOrganizationRequestDto>.HandleAsync(AddOrganizationRequestDto @params)
    {
        try
        {
            if (@params is null)
                return ResultExceptionFactory.Error($"{nameof(AddOrganizationRequestDto)} object is null", HttpStatusCode.BadRequest);

            // Validate
            DtoValidationHelper<AddOrganizationRequestDto, AddOrganizationValidator> dtoValidationHelper =
                new DtoValidationHelper<AddOrganizationRequestDto, AddOrganizationValidator>();

            AddOrganizationRequestDto addOrganizationRequestDto = @params;

            var validationResult = await dtoValidationHelper.ValidateAsync(addOrganizationRequestDto);
            if (validationResult.IsFailed)
                return ResultExceptionFactory.Error(validationResult.Errors[0]);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}

#endregion Validation Service

#region Decrypt Service

public class AddOrganizationDecrypteAndValidateParameters
{
    public AesRequestDto? Request { get; }

    public AddOrganizationDecrypteAndValidateParameters(AesRequestDto request)
    {
        Request = request;
    }
}

public interface IAddOrganizationDecrypteService : IServiceHandlerAsync<AddOrganizationDecrypteAndValidateParameters, AddOrganizationRequestDto>
{
}

[ScopedService(typeof(IAddOrganizationDecrypteService))]
public class AddOrganizationDecrypteService : IAddOrganizationDecrypteService
{
    private readonly IConfigHelper _configHelper;

    public AddOrganizationDecrypteService(IConfigHelper configHelper)
    {
        _configHelper = configHelper;
    }

    async Task<Result<AddOrganizationRequestDto>> IServiceHandlerAsync<AddOrganizationDecrypteAndValidateParameters, AddOrganizationRequestDto>.HandleAsync(AddOrganizationDecrypteAndValidateParameters @params)
    {
        try
        {
            if (@params is null)
                return ResultExceptionFactory.Error<AddOrganizationRequestDto>($"{nameof(AddOrganizationDecrypteAndValidateParameters)} object is null", HttpStatusCode.BadRequest);

            var aesRequestDto = @params.Request;
            if (aesRequestDto is null)
                return ResultExceptionFactory.Error<AddOrganizationRequestDto>($"{nameof(AesRequestDto)} object is null", HttpStatusCode.BadRequest);

            // Get Aes Secret Value from Config Manager
            var aesSecret = _configHelper.GetValue(ConstantValue.AesSecretKey!);
            if (aesSecret.IsFailed)
                return ResultExceptionFactory.Error<AddOrganizationRequestDto>("Aes Secret Key not found", HttpStatusCode.NotFound);

            // Decrypt Request
            AesDecrypteWrapper<AddOrganizationRequestDto> aesDecrypteWrapper =
                new AesDecrypteWrapper<AddOrganizationRequestDto>();

            AesDecrypteWrapperParameter aesDecrypteWrapperParameter =
                new AesDecrypteWrapperParameter(aesRequestDto, aesSecret.Value);

            var aesDecryptionResult = await aesDecrypteWrapper.HandleAsync(aesDecrypteWrapperParameter);
            if (aesDecryptionResult.IsFailed)
                return ResultExceptionFactory.Error<AddOrganizationRequestDto>(aesDecryptionResult.Errors[0]);

            return Result.Ok(aesDecryptionResult.Value);
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error<AddOrganizationRequestDto>(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}

#endregion Decrypt Service

#region Add Organization Map Service

public interface IAddOrganizationRequestEntityMapService : IServiceHandlerAsync<AddOrganizationRequestDto, Torganization>
{
}

[ScopedService(typeof(IAddOrganizationRequestEntityMapService))]
public class AddOrganizationRequestEntityMapService : IAddOrganizationRequestEntityMapService
{
    async Task<Result<Torganization>> IServiceHandlerAsync<AddOrganizationRequestDto, Torganization>.HandleAsync(AddOrganizationRequestDto @params)
    {
        try
        {
            if (@params is null)
                return ResultExceptionFactory.Error<Torganization>($"{nameof(AddOrganizationRequestDto)} object is null", HttpStatusCode.BadRequest);

            Torganization torganization = new Torganization
            {
                Identifier = Guid.NewGuid(),
                Name = @params.Name,
                Status = Convert.ToBoolean((int)StatusEnum.Active),
                CreatedDate = DateTime.UtcNow,
            };

            return Result.Ok(torganization);
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error<Torganization>(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}

#endregion Add Organization Map Service

#region Domain Event

public class OrganizationCreatedDomainEvent : INotification
{
    public Guid Identifier { get; }

    public OrganizationCreatedDomainEvent(Guid identifier)
    {
        this.Identifier = identifier;
    }
}

public class OrganizationCreatedDomainEventHandler : INotificationHandler<OrganizationCreatedDomainEvent>
{
    private readonly IOrganizationSharedCacheService _organizationSharedCacheService;

    private readonly ILogger<OrganizationCreatedDomainEventHandler> _logger;

    public OrganizationCreatedDomainEventHandler(IOrganizationSharedCacheService organizationSharedCacheService, ILogger<OrganizationCreatedDomainEventHandler> logger)
    {
        _organizationSharedCacheService = organizationSharedCacheService;
        _logger = logger;
    }

    public async Task HandleCacheAsync(OrganizationCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var result = await _organizationSharedCacheService.HandleAsync(
            new OrganizationSharedCacheServiceParameter(notification.Identifier, cancellationToken));

        if (result.IsFailed)
            _logger.LogError(result.Errors[0].Message);
    }

    Task INotificationHandler<OrganizationCreatedDomainEvent>.Handle(OrganizationCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _ = BackgroundJob.Enqueue(() => HandleCacheAsync(notification, cancellationToken));

            return Task.CompletedTask;
        }
        catch
        {
            _logger.LogError("Error in AddOrganizationDomainEventHandler");
            throw;
        }
    }
}

#endregion Domain Event

#region Add Organization Response Service

public interface IAddOrganizationResponseService : IServiceHandlerAsync<Torganization, AesResponseDto>
{
}

[ScopedService(typeof(IAddOrganizationResponseService))]
public class AddOrganizationResponseService : IAddOrganizationResponseService
{
    private readonly IConfigHelper _configHelper;

    public AddOrganizationResponseService(IConfigHelper configHelper)
    {
        _configHelper = configHelper;
    }

    async Task<Result<AesResponseDto>> IServiceHandlerAsync<Torganization, AesResponseDto>.HandleAsync(Torganization @params)
    {
        try
        {
            if (@params is null)
                return ResultExceptionFactory.Error<AesResponseDto>($"{nameof(Torganization)} object is null", HttpStatusCode.BadRequest);

            // Get Aes Secret Value from Config Manager
            var aesSecret = _configHelper.GetValue(ConstantValue.AesSecretKey!);
            if (aesSecret.IsFailed)
                return ResultExceptionFactory.Error<AesResponseDto>("Aes Secret Key not found", HttpStatusCode.NotFound);

            AddOrganizationResponseDto addOrganizationResponseDto = new AddOrganizationResponseDto
            {
                Identifier = @params.Identifier,
            };

            // Encrypt Response
            AesEncryptWrapper<AddOrganizationResponseDto> aesEncrypteWrapper =
                new AesEncryptWrapper<AddOrganizationResponseDto>();

            var aesEncryptionResult = await aesEncrypteWrapper.HandleAsync(new AesEncrypteWrapperParameter<AddOrganizationResponseDto>(aesSecret.Value, addOrganizationResponseDto));
            if (aesEncryptionResult.IsFailed)
                return ResultExceptionFactory.Error<AesResponseDto>(aesEncryptionResult.Errors[0]);

            return Result.Ok(aesEncryptionResult.Value!);
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error<AesResponseDto>(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}

#endregion Add Organization Response Service

#region Command Handler

public class AddOrganizationCommand : IRequest<DataResponse<AesResponseDto>>
{
    public AesRequestDto? Request { get; set; }

    public AddOrganizationCommand(AesRequestDto request)
    {
        Request = request;
    }
}

public class AddOrganizationCommandHandler : IRequestHandler<AddOrganizationCommand, DataResponse<AesResponseDto>>
{
    private readonly IDataResponseFactory _dataResponseFactory;
    private readonly IAddOrganizationDecrypteService _addOrganizationDecrypteService;
    private readonly IAddOrgnizationValidationService _addOrgnizationValidationService;
    private readonly IAddOrganizationRequestEntityMapService _addOrganizationRequestEntityMapService;
    private readonly IAddOrganizationDbService _addOrganizationDbService;
    private readonly IAddOrganizationResponseService _addOrganizationResponseService;
    private readonly IMediator _mediator;

    public AddOrganizationCommandHandler(
        IDataResponseFactory dataResponseFactory,
        IAddOrganizationDecrypteService addOrganizationDecrypteService,
        IAddOrgnizationValidationService orgnizationValidationService,
        IAddOrganizationRequestEntityMapService addOrganizationRequestEntityMapService,
        IAddOrganizationDbService addOrganizationDbService,
        IAddOrganizationResponseService addOrganizationResponseService,
        IMediator mediator
        )
    {
        _dataResponseFactory = dataResponseFactory;
        _addOrganizationDecrypteService = addOrganizationDecrypteService;
        _addOrgnizationValidationService = orgnizationValidationService;
        _addOrganizationRequestEntityMapService = addOrganizationRequestEntityMapService;
        _addOrganizationDbService = addOrganizationDbService;
        _addOrganizationResponseService = addOrganizationResponseService;
        _mediator = mediator;
    }

    async Task<DataResponse<AesResponseDto>> IRequestHandler<AddOrganizationCommand, DataResponse<AesResponseDto>>.Handle(AddOrganizationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request is null)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>($"{nameof(AddOrganizationCommand)} object is null", (int)HttpStatusCode.BadRequest);

            AesRequestDto aesRequestDto = request.Request!;
            if (aesRequestDto is null)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>("Request object is null", (int)HttpStatusCode.BadRequest);

            // Decrypt
            var aesDecryptionResult = await _addOrganizationDecrypteService.HandleAsync(new AddOrganizationDecrypteAndValidateParameters(aesRequestDto));
            if (aesDecryptionResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(aesDecryptionResult.Errors[0].Message, (int)HttpStatusCode.BadRequest);

            AddOrganizationRequestDto addOrganizationRequestDto = aesDecryptionResult.Value!;

            // Validation
            var validationResult = await _addOrgnizationValidationService.HandleAsync(addOrganizationRequestDto);
            if (validationResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(validationResult.Errors[0].Message, (int)HttpStatusCode.BadRequest);

            // Map AddOraganizationRequestDTO to TOrganization Entity
            var organizationResult = await _addOrganizationRequestEntityMapService.HandleAsync(addOrganizationRequestDto);
            if (organizationResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(organizationResult.Errors[0].Message, (int)HttpStatusCode.BadRequest);

            Torganization torganization = organizationResult.Value!;

            // Add Organization
            var addOrganizationResult = await _addOrganizationDbService.HandleAsync(new AddOrganizationSqlParameters(torganization, cancellationToken));
            if (addOrganizationResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(addOrganizationResult.Errors[0].Message, (int)HttpStatusCode.BadRequest);

            torganization = addOrganizationResult.Value!;

            // Publish Domain Event
            _ = _mediator.Publish(new OrganizationCreatedDomainEvent(torganization.Identifier), cancellationToken);

            // Response
            var responseResult = await _addOrganizationResponseService.HandleAsync(torganization);
            if (responseResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(responseResult.Errors[0].Message, (int)HttpStatusCode.BadRequest);

            return await _dataResponseFactory.SuccessAsync<AesResponseDto>((int)HttpStatusCode.Created, responseResult.Value, "Organization added successfully");
        }
        catch (Exception ex)
        {
            return await _dataResponseFactory.ErrorAsync<AesResponseDto>(ex.Message, (int)HttpStatusCode.InternalServerError);
        }
    }
}

#endregion Command Handler