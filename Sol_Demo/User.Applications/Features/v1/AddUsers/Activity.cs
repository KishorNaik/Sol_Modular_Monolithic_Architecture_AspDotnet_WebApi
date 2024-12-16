
using Models.Shared.Constant;
using Models.Shared.Enums;
using Organization.Contracts.Features.AddOrganizations;
using Organization.Contracts.Shared.Events.IsOrgExists;
using User.Applications.Shared.BaseController;
using User.Applications.Shared.Cache;
using User.Applications.Shared.Services.HashPassword;
using User.Contracts.Features.AddUsers;
using Users.Infrastructures.Entities;
using Users.Infrastructures.Services.AddUsers;
using Utility.Shared.RandomString;

namespace User.Applications.Features.v1.AddUsers;

#region Controller Endpoint
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/users")]
[Tags("Users")]
public class AddUserController : UserBaseController
{
    public AddUserController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost]
    [MapToApiVersion(1)]
    [DisableRateLimiting]
    [AllowAnonymous]
    [ProducesResponseType<DataResponse<AesResponseDto>>((int)HttpStatusCode.Created)]
    [ProducesResponseType<DataResponse<AesResponseDto>>((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> CreateAsync([FromBody] AesRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = await base.Mediator.Send(new AddUserCommand(request));
        return base.StatusCode(Convert.ToInt32(response.StatusCode),response);
    }

}
#endregion

#region Validation Service

[ScopedService]
public sealed class AddUserValidator : AbstractValidator<AddUserRequestDto>
{
    public AddUserValidator()
    {
        this.FirstNameValidation();
        this.LastNameValidation();
        this.EmailIdValidation();
        this.MobileValidation();
        this.PasswordValidation();
        this.OrgIdValidation();

    }

    private void FirstNameValidation()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithErrorCode("FirstName")
            .Length(0, 50).WithMessage("First Name must be less than 50 charcters").WithErrorCode("FirstName")
            .MaximumLength(50).WithErrorCode("FirstName")
            .Matches(new Regex(@"^[a-zA-Z0-9 ]*$")).WithMessage("First Name must not contain special characters.").WithErrorCode("FirstName")
            .Must(name => !Regex.IsMatch(name, "<.*>|<.*|.*>")).WithMessage("First Name must not contain HTML tags.").WithErrorCode("FirstName")
            .Must(name => !Regex.IsMatch(name, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>")).WithMessage("First Name must not contain JavaScript.").WithErrorCode("FirstName");
    }

    private void LastNameValidation()
    {
        RuleFor(x => x.LastName)
           .NotEmpty().WithErrorCode("LastName")
           .Length(0, 50).WithMessage("Last Name must be less than 50 charcters").WithErrorCode("LastName")
           .MaximumLength(50).WithErrorCode("LastName")
           .Matches(new Regex(@"^[a-zA-Z0-9 ]*$")).WithMessage("Last Name must not contain special characters.").WithErrorCode("LastName")
           .Must(name => !Regex.IsMatch(name, "<.*>|<.*|.*>")).WithMessage("Last Name must not contain HTML tags.").WithErrorCode("LastName")
           .Must(name => !Regex.IsMatch(name, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>")).WithMessage("Last Name must not contain JavaScript.").WithErrorCode("FirstName");
    }

    private void EmailIdValidation()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode("EmailId")
            .EmailAddress().WithErrorCode("EmailId")
            .Must(name => !Regex.IsMatch(name, "<.*>|<.*|.*>")).WithMessage("Email must not contain HTML tags.").WithErrorCode("EmailId")
            .Must(name => !Regex.IsMatch(name, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>")).WithMessage("Email must not contain JavaScript.").WithErrorCode("EmailId");
    }

    private void MobileValidation()
    {
        RuleFor(x => x.Mobile)
            .NotEmpty().WithErrorCode("MobileNo")
            .Matches(@"^\d{10}$").WithMessage("Mobile no must be 10 digit").WithErrorCode("MobileNo")
            .Must(name => !Regex.IsMatch(name, "<.*>|<.*|.*>")).WithMessage("Mobile No must not contain HTML tags.").WithErrorCode("MobileNo")
            .Must(name => !Regex.IsMatch(name, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>")).WithMessage("Mobile No Name must not contain JavaScript.").WithErrorCode("MobileNo");
    }

    private void PasswordValidation()
    {
        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode("Password")
            .MinimumLength(8).WithErrorCode("Password")
            .Must(name => !Regex.IsMatch(name, "<.*>|<.*|.*>")).WithMessage("Password must not contain HTML tags.").WithErrorCode("Password")
            .Must(name => !Regex.IsMatch(name, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>")).WithMessage("Password must not contain JavaScript.").WithErrorCode("Password");
    }

    private void OrgIdValidation()
    {
        RuleFor(x => x.OrgId)
            .Empty().WithErrorCode("OrgId")
            .When(x => !x.OrgId.HasValue)
            .NotEmpty().WithErrorCode("OrgId")
            .When(x => x.OrgId.HasValue)
            .Must(x =>
            {
                Guid identifierGuid;
                var flag = Guid.TryParse(x.ToString(), out identifierGuid);

                return flag;
            })
            .When(x => x.OrgId.HasValue)
            .WithMessage("Invalid Org id")
            .WithErrorCode("OrgId");
    }

}

public interface IAddUserValidationService : IServiceHandlerVoidAsync<AddUserRequestDto>
{

}

[ScopedService(typeof(IAddUserValidationService))]
public class AddUserValidationService : IAddUserValidationService
{
    private readonly IServiceProvider _serviceProvider;

    public AddUserValidationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    async Task<Result> IServiceHandlerVoidAsync<AddUserRequestDto>.HandleAsync(AddUserRequestDto @params)
    {
        try
        { 
            if(@params is null)
                return ResultExceptionFactory.Error($"{nameof(AddUserRequestDto)} is null", HttpStatusCode.BadRequest);

            // Validate
            DtoValidationHelper<AddUserRequestDto,AddUserValidator> validator = new DtoValidationHelper<AddUserRequestDto,AddUserValidator>(_serviceProvider);

            var validationResult=await validator.ValidateAsync(@params);
            if(validationResult.IsFailed)
                return ResultExceptionFactory.Error(validationResult.Errors[0]);

            return Result.Ok();
        }
        catch(Exception ex)
        {
            return ResultExceptionFactory.Error(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}

#endregion

#region Decrypt Service
public class AddUserDecryptParameters
{
    public AesRequestDto? Request { get; }

    public AddUserDecryptParameters(AesRequestDto? request)
    {
        Request = request;
    }
}

public interface IAddUserDecryptService : IServiceHandlerAsync<AddUserDecryptParameters, AddUserRequestDto>
{

}

[ScopedService(typeof(IAddUserDecryptService))]
public sealed class AddUserDecryptService : IAddUserDecryptService
{
    private readonly IConfigHelper _configHelper = null;

    public AddUserDecryptService(IConfigHelper configHelper)
    {
        _configHelper = configHelper;
    }


    async Task<Result<AddUserRequestDto>> IServiceHandlerAsync<AddUserDecryptParameters, AddUserRequestDto>.HandleAsync(AddUserDecryptParameters @params)
    {
        try
        {
            if (@params is null)
                return ResultExceptionFactory.Error<AddUserRequestDto>($"{nameof(AddUserRequestDto)} object is null", HttpStatusCode.BadRequest);

            var aesRequestDto = @params.Request;
            if (aesRequestDto is null)
                return ResultExceptionFactory.Error<AddUserRequestDto>($"{nameof(AesRequestDto)} object is null", HttpStatusCode.BadRequest);

            // Get Aes Secret Value from Config Manager
            var aesSecret = _configHelper.GetValue(ConstantValue.AesSecretKey!);
            if (aesSecret.IsFailed)
                return ResultExceptionFactory.Error<AddUserRequestDto>("Aes Secret Key not found", HttpStatusCode.NotFound);

            // Decrypt Request
            IAesDecrypteWrapper<AddUserRequestDto> aesDecrypteWrapper =
                new AesDecrypteWrapper<AddUserRequestDto>();

            AesDecrypteWrapperParameter aesDecrypteWrapperParameter =
                new AesDecrypteWrapperParameter(aesRequestDto.Body!, aesSecret.Value);

            var aesDecryptionResult = await aesDecrypteWrapper.HandleAsync(aesDecrypteWrapperParameter);
            if (aesDecryptionResult.IsFailed)
                return ResultExceptionFactory.Error<AddUserRequestDto>(aesDecryptionResult.Errors[0]);

            return Result.Ok(aesDecryptionResult.Value);

        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error<AddUserRequestDto>(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}
#endregion

#region Entity Map Service
public class AddUserRequestEntityMapServiceParameters
{
    public AddUserRequestDto? Request { get; }

    public GenerateHashPasswordServiceResult GenerateHashPasswordResult { get; }

    public CancellationToken CancellationToken { get; set; }

    public AddUserRequestEntityMapServiceParameters(
        AddUserRequestDto? request,
        GenerateHashPasswordServiceResult generateHashPasswordResult,
        CancellationToken cancellationToken)
    {
        Request = request;
        GenerateHashPasswordResult = generateHashPasswordResult;
        CancellationToken = cancellationToken;
    }
}

public class AddUserRequestEntityMapServiceResult
{
    public Tuser? Users { get;}
    
    public AddUserRequestEntityMapServiceResult(Tuser? users)
    {
        Users = users;
    }

}

public interface IAddUserRequestEntityMapService : IServiceHandlerAsync<AddUserRequestEntityMapServiceParameters, AddUserRequestEntityMapServiceResult>
{

}

[ScopedService(typeof(IAddUserRequestEntityMapService))]
public sealed class AddUserRequestEntityMapService : IAddUserRequestEntityMapService
{
    Task<Result<AddUserRequestEntityMapServiceResult>> IServiceHandlerAsync<AddUserRequestEntityMapServiceParameters, AddUserRequestEntityMapServiceResult>.HandleAsync(AddUserRequestEntityMapServiceParameters @params)
    {
        return Task.Run<Result<AddUserRequestEntityMapServiceResult>>(() =>
        {
            try
            {
                if (@params is null)
                    return ResultExceptionFactory.Error<AddUserRequestEntityMapServiceResult>($"{nameof(AddUserRequestEntityMapServiceParameters)} object is null", HttpStatusCode.BadRequest);

                if (@params?.Request is null)
                    return ResultExceptionFactory.Error<AddUserRequestEntityMapServiceResult>($"{nameof(AddUserRequestDto)} object is null", HttpStatusCode.BadRequest);

                AddUserRequestDto addUserRequestDto=@params.Request;
                GenerateHashPasswordServiceResult generateHashPasswordServiceResult=@params.GenerateHashPasswordResult;

                Tuser tuser = new Tuser();
                tuser.Identifier = Guid.NewGuid();
                tuser.FirstName = addUserRequestDto.FirstName;
                tuser.LastName = addUserRequestDto.LastName;
                tuser.Status =Convert.ToBoolean((int)StatusEnum.Inactive);
                tuser.UserType = Convert.ToInt32(UserTypeEnum.User);
                
                tuser.TuserCommunication = new TuserCommunication();
                tuser.TuserCommunication.UserId = tuser.Identifier;
                tuser.TuserCommunication.EmailId=addUserRequestDto.Email;
                tuser.TuserCommunication.MobileNumber=addUserRequestDto.Mobile;
               

                tuser.TuserCredential = new TuserCredential();
                tuser.TuserCredential.UserId=tuser.Identifier;
                tuser.TuserCredential.ClientId = Guid.NewGuid();
                tuser.TuserCredential.AesSecretKey= RandomComplexSring.GenerateComplexRandomString();
                tuser.TuserCredential.HmacSecretKey = RandomComplexSring.GenerateComplexRandomString();
                tuser.TuserCredential.Hash = generateHashPasswordServiceResult.Hash;
                tuser.TuserCredential.Salt = generateHashPasswordServiceResult.Salt;

                tuser.TuserSetting = new TuserSetting();
                tuser.TuserSetting.UserId=tuser.Identifier;
                tuser.TuserSetting.IsEmailVerified =Convert.ToBoolean((int)VerifiedEnum.No);

                tuser.TuserToken = new TuserToken();
                tuser.TuserToken.UserId=tuser.Identifier;
                tuser.TuserToken.EmailToken = Guid.NewGuid();
                
                tuser.TusersOrganization = new TusersOrganization();
                tuser.TusersOrganization.UserId=tuser.Identifier;
                tuser.TusersOrganization.OrgId =Guid.Parse(addUserRequestDto.OrgId.ToString()!);

                return Result.Ok(new AddUserRequestEntityMapServiceResult(tuser));

            }
            catch (Exception ex)
            {
                return ResultExceptionFactory.Error<AddUserRequestEntityMapServiceResult>(ex.Message, HttpStatusCode.InternalServerError);
            }
        },@params.CancellationToken);
    }
}


#endregion 

#region Domain Event Service
public class UserCreatedDomainEvent : INotification
{
    
    public Guid? Identifier { get; }

    public StatusEnum Status { get; }

    public UserCreatedDomainEvent(Guid identifier, StatusEnum status)
    {
        Identifier = identifier;
        Status = status;
    }
}

public sealed class UserCreatedDomainEventHandler : INotificationHandler<UserCreatedDomainEvent>
{

    private readonly ILogger<UserCreatedDomainEventHandler> _logger = null;
    private readonly IUserSharedCacheService _userSharedCacheService = null;

    public UserCreatedDomainEventHandler(ILogger<UserCreatedDomainEventHandler> logger, IUserSharedCacheService userSharedCacheService)
    {
        _logger = logger;
        _userSharedCacheService = userSharedCacheService;
    }

    public async Task HandleCacheAsync(UserCreatedDomainEvent notification,CancellationToken cancellationToken)
    {
        var result=await _userSharedCacheService.HandleAsync(
            new UserSharedCacheServiceParameters(notification.Identifier,(StatusEnum)Convert.ToInt32(notification?.Status),cancellationToken));

        if (result.IsFailed)
        {
            _logger.LogError(result.Errors[0].Message);
            return;
        }

        _logger.LogInformation($"UserCreatedDomainEventHandler Cache updated: {result.Value.IsCached}");

    }

    Task INotificationHandler<UserCreatedDomainEvent>.Handle(UserCreatedDomainEvent notification, CancellationToken cancellationToken)
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
#endregion

#region Command Handler Service
public class AddUserCommand : IRequest<DataResponse<AesResponseDto>>
{
    public AesRequestDto? Request { get; }

    public AddUserCommand(AesRequestDto? request)
    {
        Request = request;
    }
}

public sealed class AddUserCommandHandler : IRequestHandler<AddUserCommand, DataResponse<AesResponseDto>>
{

    private readonly IDataResponseFactory _dataResponseFactory = null;
    private readonly IMediator _mediator=null;
    private readonly IAddUserDecryptService _addUserDecryptService = null;
    private readonly IAddUserValidationService _addUserValidationService = null;
    private readonly IGenerateHashPasswordService _generateHashPasswordService = null;
    private readonly IAddUserRequestEntityMapService _addUserRequestEntityMapService = null;
    private readonly IAddUserDbService _addUserDbService = null;

    public AddUserCommandHandler(
        IDataResponseFactory dataResponseFactory,
        IMediator mediator,
        IAddUserDecryptService addUserDecryptService,
        IAddUserValidationService addUserValidationService,
        IGenerateHashPasswordService generateHashPasswordService,
        IAddUserRequestEntityMapService addUserRequestEntityMapService,
        IAddUserDbService addUserDbService
        )
    {
        _dataResponseFactory = dataResponseFactory;
        _mediator = mediator;
        _addUserDecryptService = addUserDecryptService;
        _addUserValidationService = addUserValidationService;
        _generateHashPasswordService = generateHashPasswordService;
        _addUserRequestEntityMapService = addUserRequestEntityMapService;
        _addUserDbService = addUserDbService;
    }

    async Task<DataResponse<AesResponseDto>> IRequestHandler<AddUserCommand, DataResponse<AesResponseDto>>.Handle(AddUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if(request is null)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>($"{nameof(AddUserCommand)} object is null", (int)HttpStatusCode.BadRequest);

            var aesRequestDto = request.Request;
            if (aesRequestDto is null)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>($"{nameof(AesRequestDto)} object is null", (int)HttpStatusCode.BadRequest);

            // Decrypt Request
            var aesDecryptionResult = await _addUserDecryptService.HandleAsync(new AddUserDecryptParameters(aesRequestDto));
            if (aesDecryptionResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(aesDecryptionResult.Errors[0].Message, (int)aesDecryptionResult.Errors[0].Metadata[ConstantValue.StatusCode]);

            AddUserRequestDto addUserRequestDto= aesDecryptionResult.Value;

            // Validate Request
            var validationResult = await _addUserValidationService.HandleAsync(addUserRequestDto);
            if (validationResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(validationResult.Errors[0].Message, (int)validationResult.Errors[0].Metadata[ConstantValue.StatusCode]);

            // Check Org Exists or not
            Guid orgId = (Guid)addUserRequestDto.OrgId!;
            var orgExistsResult = await _mediator.Send(new IsOrganizationExistsIntegrationEventService(new IsOrganizationExistsRequestDto()
            {
                Identifier = orgId,
            }));

            if(orgExistsResult is null)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>($"{nameof(IsOrganizationExistsIntegrationEventService)} object is null", (int)HttpStatusCode.BadRequest);

            if(!(bool)orgExistsResult?.Success!)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(orgExistsResult?.Message!, (int)orgExistsResult?.StatusCode!);

            // Generate Hash Password
            var hashPasswordResult = await _generateHashPasswordService.HandleAsync(addUserRequestDto.Password!);
            if (hashPasswordResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(hashPasswordResult.Errors[0].Message, (int)hashPasswordResult.Errors[0].Metadata[ConstantValue.StatusCode]);

            GenerateHashPasswordServiceResult generateHashPasswordServiceResult = hashPasswordResult.Value;

            // Request To Entity Map
            var requestToEntityMapResult = await _addUserRequestEntityMapService.HandleAsync
                (new AddUserRequestEntityMapServiceParameters(addUserRequestDto, generateHashPasswordServiceResult,cancellationToken)
                );
            if (requestToEntityMapResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(requestToEntityMapResult.Errors[0].Message, (int)requestToEntityMapResult.Errors[0].Metadata[ConstantValue.StatusCode]);

            Tuser tuser = requestToEntityMapResult.Value.Users!;

            // Add User and associate Tables
            var addUserResult = await _addUserDbService.HandleAsync(new AddUserDbServiceSqlParameters(tuser, cancellationToken));
            if (addUserResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(addUserResult.Errors[0].Message, (int)addUserResult.Errors[0].Metadata[ConstantValue.StatusCode]);

            // Publish Domain Event
            _=_mediator.Publish(new UserCreatedDomainEvent(tuser.Identifier,(StatusEnum)Convert.ToInt32(tuser.Status)), cancellationToken);

            // Response
            return await _dataResponseFactory.SuccessAsync<AesResponseDto>((int)HttpStatusCode.Created,null,"User Added Successfully");

        }
        catch(Exception ex)
        {
            return await _dataResponseFactory.ErrorAsync<AesResponseDto>(ex.Message, (int)HttpStatusCode.InternalServerError);
        }
    }
}
#endregion 