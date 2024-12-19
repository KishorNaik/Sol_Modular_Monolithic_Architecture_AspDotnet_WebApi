using Microsoft.AspNetCore.Mvc.Infrastructure;
using Models.Shared.Constant;
using Models.Shared.Enums;
using User.Applications.Features.v1.AddUsers;
using User.Applications.Shared.BaseController;
using User.Applications.Shared.Cache;
using User.Contracts.Features.EmailVerification;
using Users.Infrastructures.Contexts;
using Users.Infrastructures.Entities;
using Users.Infrastructures.Services.GetUserDataByEmailToken;
using Users.Infrastructures.Services.UpdateUserStatus;

namespace User.Applications.Features.v1.EmailVerification;


#region Controller Endpoint
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/users")]
[Tags("Users")]

public class UserEmailVerificationController : UserBaseController
{
    public UserEmailVerificationController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet("email-verification/{token}")]
    [MapToApiVersion(1)]
    [DisableRateLimiting]
    [AllowAnonymous]
    [ProducesResponseType<DataResponse<AesResponseDto>>((int)HttpStatusCode.OK)]
    [ProducesResponseType<DataResponse<AesResponseDto>>((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> EmailVerificationAsync([FromRoute] UserEmailVerificationRequestDto request,CancellationToken cancellationToken = default)
    {
        var response=await base.Mediator.Send(new UserEmailVerificationCommand(request), cancellationToken);
        return base.StatusCode(Convert.ToInt32(response.StatusCode), response);
    }
}
#endregion

#region Validation Service
[ScopedService]
public class UserEmailVerificationValidator : AbstractValidator<UserEmailVerificationRequestDto>
{
    private readonly IActionContextAccessor _actionContextAccessor;

    public UserEmailVerificationValidator(IActionContextAccessor actionContextAccessor)
    {
        _actionContextAccessor = actionContextAccessor;
        this.TokenValidation();
    }


    private void TokenValidation()
    {
        RuleFor(x => x.Token)
            .Must((context, id, propertyValidatorContext) =>
            {
                var token = (string)_actionContextAccessor.ActionContext.RouteData.Values.GetValueOrDefault("token")! ?? Convert.ToString(id);

                if (token is null)
                    return false;

                return true;
            })
            .WithMessage("token should not be empty")
            .WithErrorCode("Token")
            .Must((context, id, propertyValidatorContext) =>
            {
                var token = (string)_actionContextAccessor.ActionContext.RouteData.Values.GetValueOrDefault("token")! ?? Convert.ToString(id);

                Guid tokenGuid;
                var flag = Guid.TryParse(token, out tokenGuid);

                return flag;
            })
            .WithMessage("Token should be guid")
            .WithErrorCode("Token");
    }

}

public interface IUserEmailVerificationValidationService: IServiceHandlerVoidAsync<UserEmailVerificationRequestDto>
{

}

[ScopedService(typeof(IUserEmailVerificationValidationService))]
public class UserEmailVerificationValidationService : IUserEmailVerificationValidationService
{
    private readonly IServiceProvider _serviceProvider = null;

    public UserEmailVerificationValidationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    async Task<Result> IServiceHandlerVoidAsync<UserEmailVerificationRequestDto>.HandleAsync(UserEmailVerificationRequestDto @params)
    {
        try
        {
            if (@params is null)
                return ResultExceptionFactory.Error($"{nameof(UserEmailVerificationRequestDto)} is null", HttpStatusCode.BadRequest);

            // Validate
            DtoValidationHelper<UserEmailVerificationRequestDto, UserEmailVerificationValidator> dtoValidationHelper =
                new DtoValidationHelper<UserEmailVerificationRequestDto, UserEmailVerificationValidator>(_serviceProvider);

            var validationResult = await dtoValidationHelper.ValidateAsync(@params);
            if (validationResult.IsFailed)
                return ResultExceptionFactory.Error(validationResult.Errors[0].Message, HttpStatusCode.BadRequest);

            return Result.Ok();
        }
        catch(Exception ex)
        {
            return ResultExceptionFactory.Error(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}

#endregion

#region Domain Event
public class UserEmailVerifiedDominEvent : INotification
{
    public Guid? Identifier { get; }

    public StatusEnum Status { get; }

    public UserEmailVerifiedDominEvent(Guid? identifier, StatusEnum status)
    {
        Identifier = identifier;
        Status = status;
    }
}

public class UserEmailVerifiedDomainEventHandler : INotificationHandler<UserEmailVerifiedDominEvent>
{
    private readonly IUserSharedCacheService _userSharedCacheService;
    private readonly ILogger<UserCreatedDomainEventHandler> _logger = null;

    public UserEmailVerifiedDomainEventHandler(IUserSharedCacheService userSharedCacheService,ILogger<UserCreatedDomainEventHandler> logger)
    {
        _userSharedCacheService = userSharedCacheService;
        _logger = logger;
    }

    public async Task HandleCacheAsync(UserEmailVerifiedDominEvent notification, CancellationToken cancellationToken)
    {
        var result = await _userSharedCacheService.HandleAsync(
            new UserSharedCacheServiceParameters(notification.Identifier, (StatusEnum)Convert.ToInt32(notification?.Status), cancellationToken));

        if (result.IsFailed)
        {
            _logger.LogError(result.Errors[0].Message);
            return;
        }

        _logger.LogInformation($"UserCreatedDomainEventHandler Cache updated: {result.Value.IsCached}");

    }

    Task INotificationHandler<UserEmailVerifiedDominEvent>.Handle(UserEmailVerifiedDominEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _ = BackgroundJob.Enqueue(() => HandleCacheAsync(notification, cancellationToken));

            return Task.CompletedTask;
        }
        catch(Exception ex)
        {
            _logger.LogError("Error in UserEmailVerifiedDomainEventHandler");
            throw;
        }
    }
}

#endregion 

#region Command Handler
public class UserEmailVerificationCommand : IRequest<DataResponse<AesResponseDto>>
{
    public UserEmailVerificationRequestDto Request { get; }

    public UserEmailVerificationCommand(UserEmailVerificationRequestDto request)
    {
        Request = request;
    }
}

public class UserEmailVerificationCommandHandler : IRequestHandler<UserEmailVerificationCommand, DataResponse<AesResponseDto>>
{
    private readonly IDataResponseFactory _dataResponseFactory;
    private readonly UsersDbContext _dbContext;
    private readonly IUserEmailVerificationValidationService _userEmailVerificationValidationService;
    private readonly IGetUserDataByEmailTokenDbService _getUserDataByEmailTokenDbService;
    private readonly IUpdateUserStatusDbService _updateUserStatusDbService;
    private readonly IMediator _mediator;

    public UserEmailVerificationCommandHandler(
            IDataResponseFactory dataResponseFactory,
            UsersDbContext dbContext,
            IUserEmailVerificationValidationService userEmailVerificationValidationService,
            IGetUserDataByEmailTokenDbService getUserDataByEmailTokenDbService,
            IUpdateUserStatusDbService updateUserStatusDbService,
            IMediator mediator
        )
    {
        _dataResponseFactory = dataResponseFactory;
        _dbContext = dbContext;
        _userEmailVerificationValidationService = userEmailVerificationValidationService;
        _getUserDataByEmailTokenDbService = getUserDataByEmailTokenDbService;
        _updateUserStatusDbService = updateUserStatusDbService;
        _mediator = mediator;
    }

    async Task<DataResponse<AesResponseDto>> IRequestHandler<UserEmailVerificationCommand, DataResponse<AesResponseDto>>
        .Handle(UserEmailVerificationCommand request, CancellationToken cancellationToken)
    {

        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
 
        try
        { 
            if(request is null)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>($"{nameof(UserEmailVerificationCommand)} is null", Convert.ToInt32(HttpStatusCode.BadRequest));

            if(request.Request is null)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>($"{nameof(request.Request)} is null", Convert.ToInt32(HttpStatusCode.BadRequest));

            UserEmailVerificationRequestDto userEmailVerificationRequestDto = request.Request;

            // ValidationService
            var validationServiceResult=await _userEmailVerificationValidationService.HandleAsync(userEmailVerificationRequestDto);
            if(validationServiceResult.IsFailed)
                return await _dataResponseFactory
                    .ErrorAsync<AesResponseDto>(
                        validationServiceResult.Errors[0].Message, 
                        Convert.ToInt32(validationServiceResult.Errors[0].Metadata[ConstantValue.StatusCode]
                    )
                );

            // Get User Data by Email Token
            var getUserDataByEmailTokenResult=await _getUserDataByEmailTokenDbService
                .HandleAsync(
                    new GetUserDataByEmailTokenDbServiceSqlParameters(
                            token:userEmailVerificationRequestDto.Token,
                            cancellationToken:cancellationToken
                        )
                );
            if(getUserDataByEmailTokenResult.IsFailed)
            {
                await transaction.RollbackAsync(cancellationToken);
                return await _dataResponseFactory
                    .ErrorAsync<AesResponseDto>(
                        getUserDataByEmailTokenResult.Errors[0].Message,
                        Convert.ToInt32(getUserDataByEmailTokenResult.Errors[0].Metadata[ConstantValue.StatusCode]
                    )
                );
            }
                

            GetUserDataByEmailTokenDbServiceResult getUserDataByEmailTokenDbServiceResult = getUserDataByEmailTokenResult.Value;
            Tuser tuser = getUserDataByEmailTokenDbServiceResult.User;

            // Update User Status
            var updateUserStatusResult = await _updateUserStatusDbService.HandleAsync(
                    new UpdateUserStatusDbServiceSqlParameters(
                            user: tuser,
                            cancellationToken: cancellationToken
                        )
                    );
            if(updateUserStatusResult.IsFailed)
            {
                await transaction.RollbackAsync(cancellationToken);
                return await _dataResponseFactory
                    .ErrorAsync<AesResponseDto>(
                        updateUserStatusResult.Errors[0].Message,
                        Convert.ToInt32(updateUserStatusResult.Errors[0].Metadata[ConstantValue.StatusCode]
                    )
                );
            }

            // Publish UserEmailVerifiedDominEvent
            await _mediator.Publish(new UserEmailVerifiedDominEvent(tuser.Identifier, StatusEnum.Active), cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return await _dataResponseFactory.SuccessAsync<AesResponseDto>(
                    statusCode:Convert.ToInt32(HttpStatusCode.OK),
                    data: null,
                    message:"Email Verification Success"
                );
            
        }
        catch(Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return await _dataResponseFactory.ErrorAsync<AesResponseDto>(ex.Message,Convert.ToInt32(HttpStatusCode.InternalServerError));
        }
    }
}
#endregion 