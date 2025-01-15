using Microsoft.AspNetCore.Mvc.Infrastructure;
using Models.Shared.Constant;
using Models.Shared.Enums;
using User.Applications.Shared.BaseController;
using User.Applications.Shared.Cache;
using User.Contracts.Features.GetUserByIdentifer;
using User.Contracts.Shared.Dtos;
using User.Shared.Services.HmacSignature;
using Users.Infrastructures.Entities;
using Users.Infrastructures.Services.GetUsersByIdentifier;

namespace User.Applications.Features.v1.GetUserByIdentifer;


#region Controller Endpoint

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/users")]
[Tags("Users")]
public class GetUserByIdentifierController : UserBaseController
{

    public GetUserByIdentifierController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet("{identifier}")]
    [MapToApiVersion(1)]
    [DisableRateLimiting]
    //[AllowAnonymous]
    [Authorize(Policy = ConstantValue.UserOnlyPolicy)]
    [HmacSignatureValidationService]
    [ProducesResponseType<DataResponse<AesResponseDto>>((int)HttpStatusCode.OK)]
    [ProducesResponseType<DataResponse<AesResponseDto>>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<DataResponse<AesResponseDto>>((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> GetAsync([FromRoute] GetUserByIdentiferRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = await base.Mediator.Send(new GetUserByIdentifierQuery(request), cancellationToken);
        return base.StatusCode(Convert.ToInt32(response.StatusCode), response);
    }
}
#endregion

#region Validation Service
[ScopedService]
public class GetUserByIdentifierValidator : AbstractValidator<GetUserByIdentiferRequestDto>
{
    private readonly IActionContextAccessor actionContextAccessor;

    public GetUserByIdentifierValidator(IActionContextAccessor actionContextAccessor)
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

public interface IGetUserByIdentiferValidationService : IServiceHandlerVoidAsync<GetUserByIdentiferRequestDto>
{

}

[ScopedService(typeof(IGetUserByIdentiferValidationService))]
public sealed class GetUserByIdentiferValidationService : IGetUserByIdentiferValidationService
{
    private readonly IServiceProvider _serviceProvider = null;

    public GetUserByIdentiferValidationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    async Task<Result> IServiceHandlerVoidAsync<GetUserByIdentiferRequestDto>.HandleAsync(GetUserByIdentiferRequestDto @params)
    {
        try
        {
            if (@params is null)
                return ResultExceptionFactory.Error($"{nameof(GetUserByIdentiferRequestDto)} object is null", HttpStatusCode.BadRequest);

            // Validation
            var dtoValidationHelper = new DtoValidationHelper<GetUserByIdentiferRequestDto, GetUserByIdentifierValidator>(_serviceProvider);

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
#endregion 

#region Response Service
public class GetUserByIdentifierResponseServiceParameters
{
    public Tuser User { get; }

    public GetUserByIdentifierResponseServiceParameters(
        Tuser user
        )
    {
        User = user;
    }
}

public interface IGetUserByIdentifierResponseService: IServiceHandlerAsync<GetUserByIdentifierResponseServiceParameters, AesResponseDto> 
{ 

}

[ScopedService(typeof(IGetUserByIdentifierResponseService))]
public sealed class GetUserByIdentifierResponseService : IGetUserByIdentifierResponseService
{
    async Task<Result<AesResponseDto>> IServiceHandlerAsync<GetUserByIdentifierResponseServiceParameters, AesResponseDto>.HandleAsync(GetUserByIdentifierResponseServiceParameters @params)
    {

        try
        {
            if (@params is null)
                return ResultExceptionFactory.Error<AesResponseDto>($"{nameof(GetUserByIdentifierResponseServiceParameters)} is null", HttpStatusCode.BadRequest);

            if(@params.User is null)
                return ResultExceptionFactory.Error<AesResponseDto>($"{nameof(@params.User)} is null", HttpStatusCode.BadRequest);

            // Get Aes Secret Key from User Db
            string aseSecret = @params!.User!.TuserCredential!.AesSecretKey!;
            if(String.IsNullOrEmpty(aseSecret))
                return ResultExceptionFactory.Error<AesResponseDto>("Aes Secret Key is empty", HttpStatusCode.NotFound);

            Tuser user = @params.User!;

            // Map
            GetUserByIdentiferResponseDto getUserByIdentiferResponseDto = new GetUserByIdentiferResponseDto();
            getUserByIdentiferResponseDto.Identifier= user.Identifier!;
            
            getUserByIdentiferResponseDto.User = new UserDto();
            getUserByIdentiferResponseDto.User.FirstName= user!.FirstName!;
            getUserByIdentiferResponseDto.User.LastName= user.LastName!;
            getUserByIdentiferResponseDto.User.UserType= (UserTypeEnum)Convert.ToInt32(user.UserType!);

            getUserByIdentiferResponseDto.Communication = new UserCommunicationDto();
            getUserByIdentiferResponseDto.Communication.EmailId= user.TuserCommunication.EmailId!;
            getUserByIdentiferResponseDto.Communication.MobileNumber=user.TuserCommunication!.MobileNumber!;

            getUserByIdentiferResponseDto.Organization = new UserOrganizationDto();
            getUserByIdentiferResponseDto.Organization.OrgId= user.TusersOrganization!.OrgId!;

            // Encrypt Response
            IAesEncrypteWrapper<GetUserByIdentiferResponseDto> aesEncrypteWrapper =
            new AesEncryptWrapper<GetUserByIdentiferResponseDto>();

            var aesEncryptionResult = await aesEncrypteWrapper.HandleAsync(new AesEncrypteWrapperParameter<GetUserByIdentiferResponseDto>(aseSecret, getUserByIdentiferResponseDto));
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
#endregion 

#region Query Handler Service
public class GetUserByIdentifierQuery : IRequest<DataResponse<AesResponseDto>>
{
    public GetUserByIdentiferRequestDto? Request { get; }

    public GetUserByIdentifierQuery(GetUserByIdentiferRequestDto request)
    {
        Request = request;
    }
}

public class GetUserByIdentifierQueryHandler : IRequestHandler<GetUserByIdentifierQuery, DataResponse<AesResponseDto>>
{
    private readonly IDataResponseFactory _dataResponseFactory = null;
    private readonly IGetUserByIdentiferValidationService _getUserByIdentifierValidationService = null;
    private readonly IUserSharedCacheService _userSharedCacheService = null;
    private readonly IGetUserByIdentifierResponseService _getUserByIdentifierResponseService = null;

    public GetUserByIdentifierQueryHandler(
        IDataResponseFactory dataResponseFactory, 
        IGetUserByIdentiferValidationService getUserByIdentifierValidationService,
        IUserSharedCacheService userSharedCacheService,
        IGetUserByIdentifierResponseService getUserByIdentifierResponseService
        )
    {
        _dataResponseFactory = dataResponseFactory;
        _getUserByIdentifierValidationService = getUserByIdentifierValidationService;
        _userSharedCacheService = userSharedCacheService;
        _getUserByIdentifierResponseService = getUserByIdentifierResponseService;
    }

    async Task<DataResponse<AesResponseDto>> IRequestHandler<GetUserByIdentifierQuery, DataResponse<AesResponseDto>>.Handle(GetUserByIdentifierQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if(request is null)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>("Request is null", (int)HttpStatusCode.BadRequest);

            if(request.Request is null)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>("Request Object body is null", (int)HttpStatusCode.BadRequest);

            // Validation Service
            var validationResult = await _getUserByIdentifierValidationService.HandleAsync(request.Request);
            if(validationResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(validationResult.Errors[0].Message, (int)validationResult.Errors[0].Metadata[ConstantValue.StatusCode]);

            Guid? identifer = request.Request.Identifier;
            StatusEnum statusEnum= StatusEnum.Active;

            // Get User Data
            var getUserResult=await _userSharedCacheService.HandleAsync(
                    new UserSharedCacheServiceParameters(identifer,statusEnum,cancellationToken)
                );
            if (getUserResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(getUserResult.Errors[0].Message, (int)getUserResult.Errors[0].Metadata[ConstantValue.StatusCode]);

            Tuser user = getUserResult.Value.User!;

            // Response
            var responseResult = await _getUserByIdentifierResponseService.HandleAsync(new GetUserByIdentifierResponseServiceParameters(user));
            if (responseResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(responseResult.Errors[0].Message, (int)responseResult.Errors[0].Metadata[ConstantValue.StatusCode]);


            return await _dataResponseFactory.SuccessAsync<AesResponseDto>((int)HttpStatusCode.OK, responseResult.Value!, "User Data Retrieved Successfully");

        }
        catch (Exception ex)
        {
            return await _dataResponseFactory.ErrorAsync<AesResponseDto>(ex.Message, (int)HttpStatusCode.InternalServerError);
        }
    }
}
#endregion 