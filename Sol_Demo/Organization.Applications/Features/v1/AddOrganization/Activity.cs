

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

    [HttpPost()]
    [MapToApiVersion(1)]
    [DisableRateLimiting]
    [AllowAnonymous]
    [ProducesResponseType<DataResponse<AesResponseDto>>((int)HttpStatusCode.Created)]
    [ProducesResponseType<DataResponse<AesResponseDto>>((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> CreateAsync([FromBody] AesRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = await this.Mediator.Send(new AddOrganizationCommand(request), cancellationToken);
        return base.StatusCode(Convert.ToInt32(response.StatusCode), response);
    }
}

#endregion Controller

#region Validation Service

[ScopedService]
public sealed class AddOrganizationValidator : AbstractValidator<AddOrganizationRequestDto>
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
public sealed class AddOrgnizationValidationService : IAddOrgnizationValidationService
{
    private readonly IServiceProvider _serviceProvider;

    public AddOrgnizationValidationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    async Task<Result> IServiceHandlerVoidAsync<AddOrganizationRequestDto>.HandleAsync(AddOrganizationRequestDto @params)
    {
        try
        {
            if (@params is null)
                return ResultExceptionFactory.Error($"{nameof(AddOrganizationRequestDto)} object is null", HttpStatusCode.BadRequest);

            // Validate
            DtoValidationHelper<AddOrganizationRequestDto, AddOrganizationValidator> dtoValidationHelper =
                new DtoValidationHelper<AddOrganizationRequestDto, AddOrganizationValidator>(_serviceProvider);

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

public class AddOrganizationDecrypteParameters
{
    public AesRequestDto? Request { get; }

    public AddOrganizationDecrypteParameters(AesRequestDto request)
    {
        Request = request;
    }
}

public interface IAddOrganizationDecrypteService : IServiceHandlerAsync<AddOrganizationDecrypteParameters, AddOrganizationRequestDto>
{
}

[ScopedService(typeof(IAddOrganizationDecrypteService))]
public sealed class AddOrganizationDecrypteService : IAddOrganizationDecrypteService
{
    private readonly IConfigHelper _configHelper;

    public AddOrganizationDecrypteService(IConfigHelper configHelper)
    {
        _configHelper = configHelper;
    }

    async Task<Result<AddOrganizationRequestDto>> IServiceHandlerAsync<AddOrganizationDecrypteParameters, AddOrganizationRequestDto>.HandleAsync(AddOrganizationDecrypteParameters @params)
    {
        try
        {
            if (@params is null)
                return ResultExceptionFactory.Error<AddOrganizationRequestDto>($"{nameof(AddOrganizationDecrypteParameters)} object is null", HttpStatusCode.BadRequest);

            var aesRequestDto = @params.Request;
            if (aesRequestDto is null)
                return ResultExceptionFactory.Error<AddOrganizationRequestDto>($"{nameof(AesRequestDto)} object is null", HttpStatusCode.BadRequest);

            // Get Aes Secret Value from Config Manager
            var aesSecret = _configHelper.GetValue(ConstantValue.AesSecretKey!);
            if (aesSecret.IsFailed)
                return ResultExceptionFactory.Error<AddOrganizationRequestDto>("Aes Secret Key not found", HttpStatusCode.NotFound);

            // Decrypt Request
            IAesDecrypteWrapper<AesRequestDto, AddOrganizationRequestDto> aesDecrypteWrapper =
                new AesDecrypteWrapper<AesRequestDto, AddOrganizationRequestDto>();

            AesDecrypteWrapperParameter<AesRequestDto> aesDecrypteWrapperParameter =
                new AesDecrypteWrapperParameter<AesRequestDto>(aesRequestDto, aesSecret.Value);

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

public class AddOrganizationRequestEntityMapParameters
{
    public AddOrganizationRequestDto? Request { get; }

    public CancellationToken CancellationToken { get; }

    public AddOrganizationRequestEntityMapParameters(AddOrganizationRequestDto request, CancellationToken cancellationToken)
    {
        Request = request;
        CancellationToken = cancellationToken;
    }
}

public class AddOrganizationRequestEntityMapServiceResult
{
    public Torganization? Torganization { get; }

    public AddOrganizationRequestEntityMapServiceResult(Torganization torganization)
    {
        Torganization = torganization;
    }
}

public interface IAddOrganizationRequestEntityMapService : IServiceHandlerAsync<AddOrganizationRequestEntityMapParameters, AddOrganizationRequestEntityMapServiceResult>
{
}

[ScopedService(typeof(IAddOrganizationRequestEntityMapService))]
public sealed class AddOrganizationRequestEntityMapService : IAddOrganizationRequestEntityMapService
{
    Task<Result<AddOrganizationRequestEntityMapServiceResult>> IServiceHandlerAsync<AddOrganizationRequestEntityMapParameters, AddOrganizationRequestEntityMapServiceResult>
        .HandleAsync(AddOrganizationRequestEntityMapParameters @params)
    {
        return Task.Run<Result<AddOrganizationRequestEntityMapServiceResult>>(() =>
        {
            try
            {
                if (@params is null)
                    return ResultExceptionFactory.Error<AddOrganizationRequestEntityMapServiceResult>($"{nameof(AddOrganizationRequestEntityMapParameters)} object is null", HttpStatusCode.BadRequest);

                if (@params?.Request is null)
                    return ResultExceptionFactory.Error<AddOrganizationRequestEntityMapServiceResult>($"{nameof(AddOrganizationRequestDto)} object is null", HttpStatusCode.BadRequest);

                AddOrganizationRequestDto addOrganizationRequestDto = @params.Request;

                Torganization torganization = new Torganization
                {
                    Identifier = Guid.NewGuid(),
                    Name = addOrganizationRequestDto.Name,
                    Status = Convert.ToBoolean((int)StatusEnum.Active),
                    CreatedDate = DateTime.UtcNow,
                };

                AddOrganizationRequestEntityMapServiceResult addOrganizationRequestEntityMapServiceResult
                    = new AddOrganizationRequestEntityMapServiceResult(torganization);

                return Result.Ok(addOrganizationRequestEntityMapServiceResult);
            }
            catch (Exception ex)
            {
                return ResultExceptionFactory.Error<AddOrganizationRequestEntityMapServiceResult>(ex.Message, HttpStatusCode.InternalServerError);
            }
        }, @params.CancellationToken);
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

public sealed class OrganizationCreatedDomainEventHandler : INotificationHandler<OrganizationCreatedDomainEvent>
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

        _logger.LogInformation($"AddOrganizationDomainEventHandler Cache updated: {result.Value.IsCached}");
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

/*
#region Add Organization Response Service

public class AddOrganizationResponseServiceParameters
{
    public Torganization? Torganization { get; }

    public AddOrganizationResponseServiceParameters(Torganization torganization)
    {
        Torganization = torganization;
    }
}

public interface IAddOrganizationResponseService : IServiceHandlerAsync<AddOrganizationResponseServiceParameters, AesResponseDto>
{
}

[ScopedService(typeof(IAddOrganizationResponseService))]
public sealed class AddOrganizationResponseService : IAddOrganizationResponseService
{
    private readonly IConfigHelper _configHelper;

    public AddOrganizationResponseService(IConfigHelper configHelper)
    {
        _configHelper = configHelper;
    }

    async Task<Result<AesResponseDto>> IServiceHandlerAsync<AddOrganizationResponseServiceParameters, AesResponseDto>.HandleAsync(AddOrganizationResponseServiceParameters @params)
    {
        try
        {
            if (@params is null)
                return ResultExceptionFactory.Error<AesResponseDto>($"{nameof(AddOrganizationResponseServiceParameters)} object is null", HttpStatusCode.BadRequest);

            // Get Aes Secret Value from Config Manager
            var aesSecret = _configHelper.GetValue(ConstantValue.AesSecretKey!);
            if (aesSecret.IsFailed)
                return ResultExceptionFactory.Error<AesResponseDto>("Aes Secret Key not found", HttpStatusCode.NotFound);

            if (@params?.Torganization is null)
                return ResultExceptionFactory.Error<AesResponseDto>($"{nameof(Torganization)} object is null", HttpStatusCode.BadRequest);

            AddOrganizationResponseDto addOrganizationResponseDto = new AddOrganizationResponseDto
            {
                Identifier = @params.Torganization.Identifier,
            };

            // Encrypt Response
            IAesEncrypteWrapper<AddOrganizationResponseDto> aesEncrypteWrapper =
                new AesEncryptWrapper<AddOrganizationResponseDto>();

            var aesEncryptionResult = await aesEncrypteWrapper.HandleAsync(new AesEncrypteWrapperParameter<AddOrganizationResponseDto>(aesSecret.Value, addOrganizationResponseDto));
            if (aesEncryptionResult.IsFailed)
                return ResultExceptionFactory.Error<AesResponseDto>(aesEncryptionResult.Errors[0]);

            AesResponseDto aesResponseDto = new AesResponseDto
            {
                Body = aesEncryptionResult.Value,
            };

            return Result.Ok(aesResponseDto);
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error<AesResponseDto>(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}

#endregion Add Organization Response Service
*/

#region Command Handler

public class AddOrganizationCommand : IRequest<DataResponse<AesResponseDto>>
{
    public AesRequestDto? Request { get; set; }

    public AddOrganizationCommand(AesRequestDto request)
    {
        Request = request;
    }
}

public sealed class AddOrganizationCommandHandler : IRequestHandler<AddOrganizationCommand, DataResponse<AesResponseDto>>
{
    private readonly IDataResponseFactory _dataResponseFactory;
    private readonly IAddOrganizationDecrypteService _addOrganizationDecrypteService;
    private readonly IAddOrgnizationValidationService _addOrgnizationValidationService;
    private readonly IAddOrganizationRequestEntityMapService _addOrganizationRequestEntityMapService;
    private readonly IAddOrganizationDbService _addOrganizationDbService;
    private readonly IMediator _mediator;

    public AddOrganizationCommandHandler(
        IDataResponseFactory dataResponseFactory,
        IAddOrganizationDecrypteService addOrganizationDecrypteService,
        IAddOrgnizationValidationService orgnizationValidationService,
        IAddOrganizationRequestEntityMapService addOrganizationRequestEntityMapService,
        IAddOrganizationDbService addOrganizationDbService,
        IMediator mediator
        )
    {
        _dataResponseFactory = dataResponseFactory;
        _addOrganizationDecrypteService = addOrganizationDecrypteService;
        _addOrgnizationValidationService = orgnizationValidationService;
        _addOrganizationRequestEntityMapService = addOrganizationRequestEntityMapService;
        _addOrganizationDbService = addOrganizationDbService;
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
            var aesDecryptionResult = await _addOrganizationDecrypteService.HandleAsync(new AddOrganizationDecrypteParameters(aesRequestDto));
            if (aesDecryptionResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(aesDecryptionResult.Errors[0].Message, (int)HttpStatusCode.BadRequest);

            AddOrganizationRequestDto addOrganizationRequestDto = aesDecryptionResult.Value!;

            // Validation
            var validationResult = await _addOrgnizationValidationService.HandleAsync(addOrganizationRequestDto);
            if (validationResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(validationResult.Errors[0].Message, (int)HttpStatusCode.BadRequest);

            // Map AddOraganizationRequestDTO to TOrganization Entity
            var organizationResult = await _addOrganizationRequestEntityMapService
                .HandleAsync(new AddOrganizationRequestEntityMapParameters(addOrganizationRequestDto, cancellationToken));
            if (organizationResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(organizationResult.Errors[0].Message, (int)HttpStatusCode.BadRequest);

            Torganization torganization = organizationResult.Value.Torganization!;

            // Add Organization
            var addOrganizationResult = await _addOrganizationDbService.HandleAsync(new AddOrganizationSqlParameters(torganization, cancellationToken));
            if (addOrganizationResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(addOrganizationResult.Errors[0].Message, (int)HttpStatusCode.BadRequest);

            torganization = addOrganizationResult.Value!;

            // Publish Domain Event
            _ = _mediator.Publish(new OrganizationCreatedDomainEvent(torganization.Identifier), cancellationToken);

            // Response
           
            return await _dataResponseFactory.SuccessAsync<AesResponseDto>((int)HttpStatusCode.Created, null, "Organization added successfully");
        }
        catch (Exception ex)
        {
            return await _dataResponseFactory.ErrorAsync<AesResponseDto>(ex.Message, (int)HttpStatusCode.InternalServerError);
        }
    }
}

#endregion Command Handler