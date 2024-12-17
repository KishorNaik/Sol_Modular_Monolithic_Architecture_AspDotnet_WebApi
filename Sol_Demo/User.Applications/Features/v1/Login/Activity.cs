using Frameworks.Aspnetcore.Library.Extensions;
using MassTransit.Internals.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Models.Shared.Constant;
using Models.Shared.Enums;
using Newtonsoft.Json;
using System.Security.Claims;
using User.Applications.Shared.BaseController;
using User.Applications.Shared.Cache;
using User.Contracts.Features.AddUsers;
using User.Contracts.Features.GetUserByIdentifer;
using User.Contracts.Features.Login;
using User.Contracts.Shared.Dtos;
using Users.Infrastructures.Contexts;
using Users.Infrastructures.Entities;
using Users.Infrastructures.Services.GetUsersByIdentifier;
using Users.Infrastructures.Services.UpdateRefreshToken;
using Users.Infrastructures.Services.UpdateUseRowVersion;
using Utility.Shared.Cache;

namespace User.Applications.Features.v1.Login;


#region Controller Endpoint
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/users")]
[Tags("Users")]
public class UserLoginController : UserBaseController
{
    public UserLoginController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost("login")]
    [MapToApiVersion(1)]
    [DisableRateLimiting]
    [AllowAnonymous]
    [ProducesResponseType<DataResponse<AesResponseDto>>((int)HttpStatusCode.OK)]
    [ProducesResponseType<DataResponse<AesResponseDto>>((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> LoginAsync([FromBody] AesRequestDto aesRequestDto, CancellationToken cancellationToken)
    {
        var response = await base.Mediator.Send(new UserLoginCommand(aesRequestDto), cancellationToken);
        return base.StatusCode(Convert.ToInt32(response.StatusCode), response);
    }
    
}
#endregion

#region Decrypt Service
public class UserLoginDecrypServiceParameters
{
    public AesRequestDto AesRequestDto { get; }

    public UserLoginDecrypServiceParameters(AesRequestDto aesRequestDto)
    {
        AesRequestDto = aesRequestDto;
    }
}

public interface IUserLoginDecrypService: IServiceHandlerAsync<UserLoginDecrypServiceParameters, UserLoginRequestDto>
{

}

[ScopedService(typeof(IUserLoginDecrypService))]
public class UserLoginDecrypService : IUserLoginDecrypService
{
    private readonly IConfigHelper _configHelper = null;

    public UserLoginDecrypService(IConfigHelper configHelper)
    {
        _configHelper = configHelper;
    }

    async Task<Result<UserLoginRequestDto>> IServiceHandlerAsync<UserLoginDecrypServiceParameters, UserLoginRequestDto>.HandleAsync(UserLoginDecrypServiceParameters @params)
    {
        try
        {
            if(@params is null)
                return ResultExceptionFactory.Error<UserLoginRequestDto>($"{nameof(UserLoginDecrypServiceParameters)} is null", HttpStatusCode.BadRequest);

            if(@params.AesRequestDto is null)
                return ResultExceptionFactory.Error<UserLoginRequestDto>($"{nameof(@params.AesRequestDto)} is null", HttpStatusCode.BadRequest);

            AesRequestDto aesRequestDto = @params.AesRequestDto;

            // Get Aes Secret Value From Config Manager
            var aesSecretResult = _configHelper.GetValue(ConstantValue.AesSecretKey!);
            if (aesSecretResult.IsFailed)
                return ResultExceptionFactory.Error<UserLoginRequestDto>(aesSecretResult.Errors[0]);

            // Decrypt Request
            IAesDecrypteWrapper<UserLoginRequestDto> aesDecrypteWrapper =
                new AesDecrypteWrapper<UserLoginRequestDto>();

            AesDecrypteWrapperParameter aesDecrypteWrapperParameter =
                new AesDecrypteWrapperParameter(aesRequestDto.Body!, aesSecretResult.Value);

            var aesDecryptionResult = await aesDecrypteWrapper.HandleAsync(aesDecrypteWrapperParameter);
            if (aesDecryptionResult.IsFailed)
                return ResultExceptionFactory.Error<UserLoginRequestDto>(aesDecryptionResult.Errors[0]);

            return Result.Ok(aesDecryptionResult.Value);
        }
        catch(Exception ex)
        {
            return ResultExceptionFactory.Error<UserLoginRequestDto>(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}

#endregion 

#region Validation Service

[ScopedService]
public class UserLoginValidator : AbstractValidator<UserLoginRequestDto>
{

    public UserLoginValidator()
    {
        this.EmailIdValidation();
        this.PasswordValidation();
    }

    private void EmailIdValidation()
    {
        RuleFor(x => x.EmailId)
            .NotEmpty().WithErrorCode("EmailId")
            .EmailAddress().WithErrorCode("EmailId");
    }

    private void PasswordValidation()
    {
        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode("Password")
            .MinimumLength(8).WithErrorCode("Password");
    }
}

public interface IUserLoginValidationService: IServiceHandlerVoidAsync<UserLoginRequestDto>
{

}

[ScopedService(typeof(IUserLoginValidationService))]
public class UserLoginValidationService : IUserLoginValidationService
{
    private readonly IServiceProvider _serviceProvider = null!;

    public UserLoginValidationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    async Task<Result> IServiceHandlerVoidAsync<UserLoginRequestDto>.HandleAsync(UserLoginRequestDto @params)
    {
        try
        {
            if(@params is null)
                return ResultExceptionFactory.Error($"{nameof(UserLoginRequestDto)} is null", HttpStatusCode.BadRequest);

            // Validate
            DtoValidationHelper<UserLoginRequestDto, UserLoginValidator> dtoValidationHelper =
                new DtoValidationHelper<UserLoginRequestDto, UserLoginValidator>(_serviceProvider);

            var validationResult = await dtoValidationHelper.ValidateAsync(@params);
            if (validationResult.IsFailed)
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

#region Get User Data By Email ID
public class GetUserDataByEmailIdServiceParameters
{
    public string EmailId { get; }

    public CancellationToken CancellationToken { get; }

    public GetUserDataByEmailIdServiceParameters(string emailId, CancellationToken cancellationToken)
    {
        EmailId = emailId;
        CancellationToken = cancellationToken;
    }

}

public class GetUserDataByEmailServiceResult
{
    public Tuser? User { get; }

    public GetUserDataByEmailServiceResult(Tuser? user)
    {
        User = user;
    }
}

public interface IGetUserDataByEmailIdService: IServiceHandlerAsync<GetUserDataByEmailIdServiceParameters, GetUserDataByEmailServiceResult>
{

}

[ScopedService(typeof(IGetUserDataByEmailIdService))]
public class GetUserDataByEmailIdService : IGetUserDataByEmailIdService
{
    private readonly IDistributedCache _distributedCache = null;

    public GetUserDataByEmailIdService(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    async Task<Result<GetUserDataByEmailServiceResult>> IServiceHandlerAsync<GetUserDataByEmailIdServiceParameters, GetUserDataByEmailServiceResult>
        .HandleAsync(GetUserDataByEmailIdServiceParameters @params)
    {
       try
        {
            if(@params is null)
                return ResultExceptionFactory.Error<GetUserDataByEmailServiceResult>($"{nameof(GetUserDataByEmailIdServiceParameters)} is null", HttpStatusCode.BadRequest);

            if(@params.EmailId is null)
                return ResultExceptionFactory.Error<GetUserDataByEmailServiceResult>($"{nameof(@params.EmailId)} is null", HttpStatusCode.BadRequest);

            // Get Email Id
            string emailId = @params.EmailId;

            // Cache Name
            string cacheEmailIdName = $"UserEmailId-{emailId}";


            var cacheResult = await SqlCacheHelper.GetCacheAsync(_distributedCache, cacheEmailIdName, @params.CancellationToken);
            if(cacheResult is null)
                return ResultExceptionFactory.Error<GetUserDataByEmailServiceResult>($"Unauthorized access", HttpStatusCode.Unauthorized);

            Tuser cacheValueResult = JsonConvert.DeserializeObject<Tuser>(cacheResult)!;

            if (cacheValueResult is null)
                return ResultExceptionFactory.Error($"Unauthorized access", HttpStatusCode.Unauthorized);

            GetUserDataByEmailServiceResult getUserDataByEmailServiceResult =
                new GetUserDataByEmailServiceResult(cacheValueResult);

            return Result.Ok(getUserDataByEmailServiceResult);
        }
        catch(Exception ex)
        {
            return ResultExceptionFactory.Error<GetUserDataByEmailServiceResult>(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}
#endregion 

#region User Login Valid Credentials Service
public class UserLoginCredentialValidServiceParameters
{
    public GetUserDataByEmailServiceResult GetUserDataByEmailResult { get; }

    public string? Password { get; }

    public UserLoginCredentialValidServiceParameters(GetUserDataByEmailServiceResult getUserDataByEmailResult, string? password)
    {
        GetUserDataByEmailResult = getUserDataByEmailResult;
        Password = password;
    }
}

public interface IUserLoginCredentialValidService : IServiceHandlerAsync<UserLoginCredentialValidServiceParameters, Unit>
{
}

[ScopedService(typeof(IUserLoginCredentialValidService))]
public class UserLoginCredentialValidService : IUserLoginCredentialValidService
{
    async Task<Result<Unit>> IServiceHandlerAsync<UserLoginCredentialValidServiceParameters, Unit>.HandleAsync(UserLoginCredentialValidServiceParameters @params)
    {
        try
        {
            if(@params is null)
                return ResultExceptionFactory.Error<Unit>($"{nameof(UserLoginCredentialValidServiceParameters)} is null", HttpStatusCode.BadRequest);

            if(@params.GetUserDataByEmailResult is null)
                return ResultExceptionFactory.Error<Unit>($"{nameof(@params.GetUserDataByEmailResult)} is null", HttpStatusCode.BadRequest);

            if(@params.Password is null)
                return ResultExceptionFactory.Error<Unit>($"{nameof(@params.Password)} is null", HttpStatusCode.BadRequest);

            GetUserDataByEmailServiceResult getUserDataByEmailResult = @params.GetUserDataByEmailResult!;

            // Check if User Status is Active or not
            StatusEnum statusEnum =(StatusEnum)Convert.ToInt32(getUserDataByEmailResult.User?.Status);
            if(statusEnum != StatusEnum.Active)
                return ResultExceptionFactory.Error<Unit>($"User is not active. Please contact admin", HttpStatusCode.NotAcceptable);

            // Check if Email is Verified or not
            bool? isEmailVerified = getUserDataByEmailResult!.User!.TuserSetting!.IsEmailVerified!;
            if(!isEmailVerified.HasValue || !isEmailVerified.Value)
                return ResultExceptionFactory.Error<Unit>($"Email is not verified. Please verify your email to login", HttpStatusCode.NotAcceptable);

            // Validate Password With Hash
            string? salt = getUserDataByEmailResult!.User!.TuserCredential!.Salt!;
            string? hash = getUserDataByEmailResult!.User!.TuserCredential!.Hash!;

            if(salt is null || hash is null)
                return ResultExceptionFactory.Error<Unit>($"Credentials is missing. Please contact admin", HttpStatusCode.NotFound);

            bool isValidLogin = await Hash.ValidateAsync(@params.Password, salt, hash, ByteRange.byte256);
            if(!isValidLogin)
                return ResultExceptionFactory.Error<Unit>($"Invalid login credentials", HttpStatusCode.Unauthorized);

            return Result.Ok();
        }
        catch(Exception ex)
        {
            return ResultExceptionFactory.Error<Unit>(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}

#endregion 

#region User Login Generate JWT and Refresh Token 
public class UserLoginGenerateJwtAndRefreshTokenServiceParameters
{
    public GetUserDataByEmailServiceResult GetUserDataByEmailResult { get; }

    public UserLoginGenerateJwtAndRefreshTokenServiceParameters(GetUserDataByEmailServiceResult getUserDataByEmailResult)
    {
        GetUserDataByEmailResult = getUserDataByEmailResult;
    }
}

public class UserLoginGenerateJwtAndRefreshTokenServiceResult
{
    public string? JwtToken { get; }

    public string? RefreshToken { get; }

    public UserLoginGenerateJwtAndRefreshTokenServiceResult(string? jwtToken, string? refreshToken)
    {
        JwtToken = jwtToken;
        RefreshToken = refreshToken;
    }
}

public interface IUserLoginGenerateJwtAndRefreshTokenService :
    IServiceHandlerAsync<UserLoginGenerateJwtAndRefreshTokenServiceParameters, UserLoginGenerateJwtAndRefreshTokenServiceResult>
{
}

[ScopedService(typeof(IUserLoginGenerateJwtAndRefreshTokenService))]
public class UserLoginGenerateJwtAndRefreshTokenService : IUserLoginGenerateJwtAndRefreshTokenService
{
    private readonly IJwtTokenService _jwtTokenService=null;
    private readonly IOptions<JwtAppSetting> _options;

    public UserLoginGenerateJwtAndRefreshTokenService(IJwtTokenService jwtTokenService, IOptions<JwtAppSetting> options)
    {
        _jwtTokenService = jwtTokenService;
        _options = options;
    }

    async Task<Result<UserLoginGenerateJwtAndRefreshTokenServiceResult>> IServiceHandlerAsync<UserLoginGenerateJwtAndRefreshTokenServiceParameters, UserLoginGenerateJwtAndRefreshTokenServiceResult>
        .HandleAsync(UserLoginGenerateJwtAndRefreshTokenServiceParameters @params)
    {
        try
        {
            if (@params is null)
                return ResultExceptionFactory
                    .Error<UserLoginGenerateJwtAndRefreshTokenServiceResult>($"{nameof(UserLoginGenerateJwtAndRefreshTokenServiceParameters)} is null", HttpStatusCode.BadRequest);

            if(@params.GetUserDataByEmailResult is null)
                return ResultExceptionFactory
                    .Error<UserLoginGenerateJwtAndRefreshTokenServiceResult>($"{nameof(@params.GetUserDataByEmailResult)} is null", HttpStatusCode.BadRequest);

            GetUserDataByEmailServiceResult gettUserDataByEmailServiceResult = @params.GetUserDataByEmailResult!;

            // Get User Identifier
            string identifier = Convert.ToString(gettUserDataByEmailServiceResult!.User!.Identifier)!;
            if(identifier is null)
                return ResultExceptionFactory.Error<UserLoginGenerateJwtAndRefreshTokenServiceResult>($"User Identifier is missing. Please contact admin", HttpStatusCode.NotFound);

            // Get User Type
            UserTypeEnum userTypeEnum = (UserTypeEnum)Convert.ToInt32(gettUserDataByEmailServiceResult!.User!.UserType)!;
            string userType=Convert.ToString(userTypeEnum)!;
            if (userType is null)
                return ResultExceptionFactory.Error<UserLoginGenerateJwtAndRefreshTokenServiceResult>($"User Type is missing. Please contact admin", HttpStatusCode.NotFound);

            // Set Claims
            List<Claim> claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.NameIdentifier, identifier));
            claims.Add(new Claim(ClaimTypes.Role, userType));

            var jwtTokenTaskResult = _jwtTokenService.GenerateJwtTokenAsync(_options.Value, claims.ToArray(), DateTime.Now.AddDays(1));
            var refreshTokenTaskResult = _jwtTokenService.GenerateRefreshTokenAsync();

            await Task.WhenAll(jwtTokenTaskResult, refreshTokenTaskResult);

            UserLoginGenerateJwtAndRefreshTokenServiceResult userLoginGenerateJwtAndRefreshTokenServiceResult =
                new UserLoginGenerateJwtAndRefreshTokenServiceResult(
                        jwtToken: jwtTokenTaskResult.Result,
                        refreshToken: refreshTokenTaskResult.Result
                    );

            return Result.Ok(userLoginGenerateJwtAndRefreshTokenServiceResult);
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error<UserLoginGenerateJwtAndRefreshTokenServiceResult>(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}
#endregion 

#region Response Service
public class UserLoginResponseServiceParameters
{
    public UserLoginGenerateJwtAndRefreshTokenServiceResult JwtAndRefreshToken { get; }

    public Tuser User { get; }

    public UserLoginResponseServiceParameters(UserLoginGenerateJwtAndRefreshTokenServiceResult jwtAndRefreshToken, Tuser user)
    {
        JwtAndRefreshToken = jwtAndRefreshToken;
        User = user;
    }
}

public interface IUserLoginResponseService : IServiceHandlerAsync<UserLoginResponseServiceParameters, AesResponseDto>
{

}

[ScopedService(typeof(IUserLoginResponseService))]
public class UserLoginResponseService : IUserLoginResponseService
{
    private readonly IConfigHelper _configHelper;

    public UserLoginResponseService(IConfigHelper configHelper)
    {
        _configHelper = configHelper;
    }

    async Task<Result<AesResponseDto>> IServiceHandlerAsync<UserLoginResponseServiceParameters, AesResponseDto>.HandleAsync(UserLoginResponseServiceParameters @params)
    {
        try
        {
            if(@params is null)
                return ResultExceptionFactory
                    .Error<AesResponseDto>($"{nameof(UserLoginResponseServiceParameters)} is null", HttpStatusCode.BadRequest);

            if(@params.User is null)
                return ResultExceptionFactory
                    .Error<AesResponseDto>($"{nameof(@params.User)} is null", HttpStatusCode.BadRequest);

            if(@params.JwtAndRefreshToken is null)
                return ResultExceptionFactory
                    .Error<AesResponseDto>($"{nameof(@params.JwtAndRefreshToken)} is null", HttpStatusCode.BadRequest);

            // Get Aes Secret Value From Config Manager
            var  aesSecretValueResult = _configHelper.GetValue(ConstantValue.AesSecretKey!);
            if(aesSecretValueResult.IsFailed)
                return ResultExceptionFactory.Error<AesResponseDto>(aesSecretValueResult.Errors[0]);

            string aesSecretValue = aesSecretValueResult.Value;

            Tuser tuser=@params.User;
            UserLoginGenerateJwtAndRefreshTokenServiceResult userLoginGenerateJwtAndRefreshTokenServiceResult = @params.JwtAndRefreshToken!;

            // Map
            UserLoginResponseDto userLoginResponseDto = new UserLoginResponseDto();
            userLoginResponseDto.Identifier = tuser.Identifier;
            userLoginResponseDto.User = new UserDto();
            userLoginResponseDto.User.FirstName = tuser.FirstName;
            userLoginResponseDto.User.LastName = tuser.LastName;
            userLoginResponseDto.User.UserType = (UserTypeEnum)Convert.ToInt32(tuser.UserType);
            userLoginResponseDto.User.Status = (StatusEnum)Convert.ToInt32(tuser.Status);

            userLoginResponseDto.JwtToken = new UseJwtTokenDto();
            userLoginResponseDto.JwtToken.RefreshToken = userLoginGenerateJwtAndRefreshTokenServiceResult.RefreshToken!;
            userLoginResponseDto.JwtToken.Token = userLoginGenerateJwtAndRefreshTokenServiceResult.JwtToken!;

            userLoginResponseDto.Communication = new UserCommunicationDto();
            userLoginResponseDto.Communication.EmailId = tuser.TuserCommunication.EmailId;
            userLoginResponseDto.Communication.MobileNumber = tuser.TuserCommunication.MobileNumber;

            // Encrypt Response
            IAesEncrypteWrapper<UserLoginResponseDto> aesEncrypteWrapper =
            new AesEncryptWrapper<UserLoginResponseDto>();

            var aesEncryptionResult = await aesEncrypteWrapper.HandleAsync(
                    new AesEncrypteWrapperParameter<UserLoginResponseDto>(
                        secret:aesSecretValue, 
                        @params:userLoginResponseDto
                    )
                );
            if (aesEncryptionResult.IsFailed)
                return ResultExceptionFactory.Error<AesResponseDto>(aesEncryptionResult.Errors[0]);

            AesResponseDto aesResponseDto = new AesResponseDto
            {
                Body = aesEncryptionResult.Value
            };

            return Result.Ok(aesResponseDto);

        }
        catch(Exception ex)
        {
            return ResultExceptionFactory.Error<AesResponseDto>(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}
#endregion 

#region Command Handler
public class UserLoginCommand : IRequest<DataResponse<AesResponseDto>>
{
    public AesRequestDto AesRequestDto { get;  }

    public UserLoginCommand(AesRequestDto aesRequestDto)
    {
        AesRequestDto = aesRequestDto;
    }
}

public class UserLoginCommandHandler : IRequestHandler<UserLoginCommand, DataResponse<AesResponseDto>>
{
    private readonly IDataResponseFactory _dataResponseFactory;
    private readonly IUserLoginDecrypService _userLoginDecrypService;
    private readonly IUserLoginValidationService _userLoginValidationService;
    private readonly IGetUserDataByEmailIdService _getUserDataByEmailIdService;
    private readonly IUserLoginCredentialValidService _userLoginCredentialValidService;
    private readonly IUserLoginGenerateJwtAndRefreshTokenService _userLoginGenerateJwtAndRefreshTokenService;
    private readonly UsersDbContext _usersDbContext;
    private readonly IUpdateRefreshTokenDbService _updateRefreshTokenDbService;
    private readonly IUpdateUserRowVersionDbService _updateUserRowVersionDbService;
    private readonly IUserSharedCacheService _userSharedCacheService;
    private readonly IUserLoginResponseService _userLoginResponseService;

    public UserLoginCommandHandler(
            IDataResponseFactory dataResponseFactory,
            IUserLoginDecrypService userLoginDecrypService,
            IUserLoginValidationService userLoginValidationService,
            IGetUserDataByEmailIdService getUserDataByEmailIdService,
            IUserLoginCredentialValidService userLoginCredentialValidService,
            IUserLoginGenerateJwtAndRefreshTokenService userLoginGenerateJwtAndRefreshTokenService,
            UsersDbContext usersDbContext,
            IUpdateRefreshTokenDbService updateRefreshTokenDbService,
            IUpdateUserRowVersionDbService updateUserRowVersionDbService,
            IUserSharedCacheService userSharedCacheService,
            IUserLoginResponseService userLoginResponseService
        )
    {
        _dataResponseFactory = dataResponseFactory;
        _userLoginDecrypService = userLoginDecrypService;
        _userLoginValidationService = userLoginValidationService;
        _getUserDataByEmailIdService = getUserDataByEmailIdService;
        _userLoginCredentialValidService = userLoginCredentialValidService;
        _userLoginGenerateJwtAndRefreshTokenService = userLoginGenerateJwtAndRefreshTokenService;
        _usersDbContext = usersDbContext;
        _updateRefreshTokenDbService = updateRefreshTokenDbService;
        _updateUserRowVersionDbService = updateUserRowVersionDbService;
        _userSharedCacheService = userSharedCacheService;
        _userLoginResponseService = userLoginResponseService;
    }

    async Task<DataResponse<AesResponseDto>> IRequestHandler<UserLoginCommand, DataResponse<AesResponseDto>>.Handle(UserLoginCommand request, CancellationToken cancellationToken)
    {
        // Update User Refresh Token
        using var transaction = await _usersDbContext.Database.BeginTransactionAsync();

        try
        {
            if(request is null)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>($"{nameof(UserLoginCommand)} is null", Convert.ToInt32(HttpStatusCode.BadRequest), null!);

            if(request.AesRequestDto is null)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>($"{nameof(request.AesRequestDto)} is null", Convert.ToInt32(HttpStatusCode.BadRequest), null!);

            // Decrypt Service
            var decryptServiceResult = await _userLoginDecrypService.HandleAsync(
                    new UserLoginDecrypServiceParameters(request.AesRequestDto)
                    );
            if (decryptServiceResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(decryptServiceResult.Errors[0].Message, Convert.ToInt32(decryptServiceResult.Errors[0].Metadata[ConstantValue.StatusCode]), null!);

            UserLoginRequestDto userLoginRequestDto = decryptServiceResult.Value;

            // Validate Service
            var validateServiceResult = await _userLoginValidationService.HandleAsync(userLoginRequestDto);
            if (validateServiceResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(validateServiceResult.Errors[0].Message, Convert.ToInt32(decryptServiceResult.Errors[0].Metadata[ConstantValue.StatusCode]), null!);

            // Get User Data by Email Id
            var getUserDataByEmailIdServiceResult = await _getUserDataByEmailIdService.HandleAsync(
                    new GetUserDataByEmailIdServiceParameters(userLoginRequestDto.EmailId!, cancellationToken)
                );
            if (getUserDataByEmailIdServiceResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(getUserDataByEmailIdServiceResult.Errors[0].Message, Convert.ToInt32(decryptServiceResult.Errors[0].Metadata[ConstantValue.StatusCode]), null!);

            GetUserDataByEmailServiceResult getUserDataByEmailServiceResult = getUserDataByEmailIdServiceResult.Value!;

            // User Login Credentials Validation Service
            var isValidLoginServiceResult = await _userLoginCredentialValidService.HandleAsync(
                    new UserLoginCredentialValidServiceParameters(getUserDataByEmailServiceResult, userLoginRequestDto.Password!)
                );
            if(isValidLoginServiceResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(getUserDataByEmailIdServiceResult.Errors[0].Message, Convert.ToInt32(decryptServiceResult.Errors[0].Metadata[ConstantValue.StatusCode]), null!);

            // Generate Jwt Claims
            var jwtClaimsResult = await _userLoginGenerateJwtAndRefreshTokenService.HandleAsync(
                    new UserLoginGenerateJwtAndRefreshTokenServiceParameters(getUserDataByEmailServiceResult)
                );
            if (jwtClaimsResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(jwtClaimsResult.Errors[0].Message, Convert.ToInt32(jwtClaimsResult.Errors[0].Metadata[ConstantValue.StatusCode]), null!);

            UserLoginGenerateJwtAndRefreshTokenServiceResult userLoginGenerateJwtAndRefreshTokenServiceResult = jwtClaimsResult.Value!;
            Tuser tuser = getUserDataByEmailServiceResult.User!;

            // Update Refresh Token
            var updateRefreshToken = await _updateRefreshTokenDbService.HandleAsync(
                    new UpdateRefreshTokenDbServiceParameters(
                           user: tuser,
                           refreshToken: userLoginGenerateJwtAndRefreshTokenServiceResult.RefreshToken!,
                           cancellationToken: cancellationToken
                        )
                    );

            if(updateRefreshToken.IsFailed)
            {
                await transaction.RollbackAsync();
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(updateRefreshToken.Errors[0].Message, Convert.ToInt32(updateRefreshToken.Errors[0].Metadata[ConstantValue.StatusCode]), null!);
            }

            // Update Row Version for Cache Refresh
            var updateRowVersion = await _updateUserRowVersionDbService.HandleAsync(
                    new UpdateUserRowVersionDbServiceParameters(
                           tuser: tuser,
                           cancellationToken: cancellationToken
                        )
                    );

            if (updateRowVersion.IsFailed)
            {
                await transaction.RollbackAsync();
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(updateRowVersion.Errors[0].Message, Convert.ToInt32(updateRowVersion.Errors[0].Metadata[ConstantValue.StatusCode]), null!);
            }

            // Update Cache
            var cacheResult=await _userSharedCacheService.HandleAsync(
                    new UserSharedCacheServiceParameters(
                          identifier: tuser.Identifier,
                          status:StatusEnum.Active,
                          cancellationToken: cancellationToken
                        )
                    );
             if(cacheResult.IsFailed)
            {
                await transaction.RollbackAsync();
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(cacheResult.Errors[0].Message, Convert.ToInt32(cacheResult.Errors[0].Metadata[ConstantValue.StatusCode]), null!);
            };

            // Response
            var response = await _userLoginResponseService.HandleAsync(
                      new UserLoginResponseServiceParameters(
                            jwtAndRefreshToken: userLoginGenerateJwtAndRefreshTokenServiceResult,
                            user: tuser
                          )
                    );
            if(response.IsFailed)
            {
                await transaction.RollbackAsync();
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(response.Errors[0].Message, Convert.ToInt32(response.Errors[0].Metadata[ConstantValue.StatusCode]), null!);
            };

            await transaction.CommitAsync();
            return await _dataResponseFactory.SuccessAsync<AesResponseDto>(
                    statusCode: Convert.ToInt32(HttpStatusCode.OK),
                    data: response.Value,
                    message: "Login Successfully"
                );

        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return await _dataResponseFactory.ErrorAsync<AesResponseDto>(ex.Message,Convert.ToInt32(HttpStatusCode.InternalServerError),null!);
        }
    }
}
#endregion 