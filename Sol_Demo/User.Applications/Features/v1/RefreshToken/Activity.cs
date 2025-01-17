using Frameworks.Aspnetcore.Library.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Models.Shared.Constant;
using Models.Shared.Enums;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using User.Applications.Features.v1.AddUsers;
using User.Applications.Shared.BaseController;
using User.Applications.Shared.Cache;
using User.Contracts.Features.AddUsers;
using User.Contracts.Features.RefreshToken;
using Users.Infrastructures.Entities;
using Users.Infrastructures.Services.UpdateRefreshToken;
using Utility.Shared.AES;
using Utility.Shared.Cache;

namespace User.Applications.Features.v1.RefreshToken;

#region Controller Endpoint
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/users")]
[Tags("Users")]
public class RefreshTokenController : UserBaseController
{
    public RefreshTokenController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost("refresh-token")]
    [MapToApiVersion(1)]
    [DisableRateLimiting]
    [AllowAnonymous]
    [ProducesResponseType<DataResponse<AesResponseDto>>((int)HttpStatusCode.Created)]
    [ProducesResponseType<DataResponse<AesResponseDto>>((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> RefreshTokenAsync([FromBody] AesRequestDto request, CancellationToken cancellationToken)
    {

        base.Request.Headers.TryGetValue("X-CLIENT-ID", out var clientId);

        var response=await base.Mediator.Send(new RefreshTokenCommand(request, clientId!), cancellationToken);
        return base.StatusCode(Convert.ToInt32(response.StatusCode), response);
    }
}
#endregion 

#region Decrypt Service
public class RefreshTokenDecryptServiceParameters
{
    public string ClientId { get; }

    public AesRequestDto Request { get;  }

    public CancellationToken CancellationToken { get; }

    public RefreshTokenDecryptServiceParameters(string clientId, AesRequestDto request, CancellationToken cancellationToken)
    {
        ClientId = clientId;
        Request = request;
        CancellationToken = cancellationToken;
    }
}

public class RefreshTokenDecryptServiceResult
{
    public RefreshTokenRequestDto Request { get; }

    public Tuser User { get; }

    public RefreshTokenDecryptServiceResult(RefreshTokenRequestDto request, Tuser user)
    {
        Request = request;
        User = user;
    }
}

public interface IRefreshTokenDecryptService : IServiceHandlerAsync<RefreshTokenDecryptServiceParameters, RefreshTokenDecryptServiceResult>
{
}

[ScopedService(typeof(IRefreshTokenDecryptService))]
public sealed class RefreshTokenDecryptService : IRefreshTokenDecryptService
{
    private readonly IDistributedCache _distributedCache;

    public RefreshTokenDecryptService(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    async Task<Result<RefreshTokenDecryptServiceResult>> IServiceHandlerAsync<RefreshTokenDecryptServiceParameters, RefreshTokenDecryptServiceResult>
        .HandleAsync(RefreshTokenDecryptServiceParameters @params)
    {
        try
        {
            if(@params is null)
                return ResultExceptionFactory.Error<RefreshTokenDecryptServiceResult>($"{nameof(RefreshTokenDecryptServiceParameters)} is null", HttpStatusCode.BadRequest);

            string clinetId=@params.ClientId;

            if (string.IsNullOrEmpty(clinetId))
                return ResultExceptionFactory.Error<RefreshTokenDecryptServiceResult>($"{nameof(clinetId)} is null or empty", HttpStatusCode.BadRequest);

            var aesRequestDto = @params.Request;

            if (aesRequestDto is null)
                return ResultExceptionFactory.Error<RefreshTokenDecryptServiceResult>($"{nameof(aesRequestDto)} is null", HttpStatusCode.BadRequest);

            // Get Aes Secret Key from User Cache
            // Cache Name
            string cacheClientId = $"UserClient-{clinetId}";

            var cacheResult = await SqlCacheHelper.GetCacheAsync(_distributedCache, cacheClientId, @params.CancellationToken);
            if(cacheResult is null)
                return ResultExceptionFactory.Error<RefreshTokenDecryptServiceResult>("Unauthorized access", HttpStatusCode.Unauthorized);

            Tuser user = JsonConvert.DeserializeObject<Tuser>(cacheResult)!;

            if (user is null)
                return ResultExceptionFactory.Error($"Unauthorized access", HttpStatusCode.Unauthorized);

            // Get Aes Secret Key from TUser
            string aesSecretKey = user.TuserCredential.AesSecretKey;

            if(aesSecretKey is null)
                return ResultExceptionFactory.Error<RefreshTokenDecryptServiceResult>($"{nameof(aesSecretKey)} is missing", HttpStatusCode.NotFound);

            // Decrypt Request
            IAesDecrypteWrapper<RefreshTokenRequestDto> aesDecrypteWrapper=new AesDecrypteWrapper<RefreshTokenRequestDto>();
            AesDecrypteWrapperParameter aesDecrypteWrapperParameter=new AesDecrypteWrapperParameter(aesRequestDto.Body!, aesSecretKey);
            var aesDecryptResult=await aesDecrypteWrapper.HandleAsync(aesDecrypteWrapperParameter);

            if(aesDecryptResult.IsFailed)
                return ResultExceptionFactory.Error<RefreshTokenDecryptServiceResult>(aesDecryptResult.Errors[0]);

            RefreshTokenDecryptServiceResult refreshTokenDecryptServiceResult=new RefreshTokenDecryptServiceResult(aesDecryptResult.Value, user);

            return Result.Ok(refreshTokenDecryptServiceResult);

        }
        catch(Exception ex)
        {
            return ResultExceptionFactory.Error<RefreshTokenDecryptServiceResult>(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}
#endregion

#region Validation Service
[ScopedService]
public sealed class RefreshTokenValidator : AbstractValidator<RefreshTokenRequestDto>
{
    public RefreshTokenValidator()
    {
        this.AccessTokenValidation();
        this.RefreshTokenValidation();
    }

    private void AccessTokenValidation()
    { 
        RuleFor(x => x.AccessToken)
            .NotEmpty().WithErrorCode("AccessToken")
            .Must(name => !Regex.IsMatch(name, "<.*>|<.*|.*>")).WithMessage("Access Token must not contain HTML tags.").WithErrorCode("AccessToken")
            .Must(name => !Regex.IsMatch(name, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>")).WithMessage("Access Token must not contain JavaScript.").WithErrorCode("AccessToken");

    }

    private void RefreshTokenValidation()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithErrorCode("RefreshToken")
            .Must(name => !Regex.IsMatch(name, "<.*>|<.*|.*>")).WithMessage("Refresh Token must not contain HTML tags.").WithErrorCode("RefreshToken")
            .Must(name => !Regex.IsMatch(name, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>")).WithMessage("Refresh Token must not contain JavaScript.").WithErrorCode("RefreshToken");

    }
}

public interface IRefreshTokenValidationService : IServiceHandlerVoidAsync<RefreshTokenRequestDto>
{

}

[ScopedService(typeof(IRefreshTokenValidationService))]
public sealed class RefreshTokenValidationService : IRefreshTokenValidationService
{
    private readonly IServiceProvider _serviceProvider;

    public RefreshTokenValidationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    async Task<Result> IServiceHandlerVoidAsync<RefreshTokenRequestDto>.HandleAsync(RefreshTokenRequestDto @params)
    {
       try
        {
            if (@params is null)
                return ResultExceptionFactory.Error($"{nameof(RefreshTokenRequestDto)} is null", HttpStatusCode.BadRequest);

            //Validate Request Dto
            DtoValidationHelper<RefreshTokenRequestDto,RefreshTokenValidator> dtoValidator=new DtoValidationHelper<RefreshTokenRequestDto, RefreshTokenValidator>(_serviceProvider);

            var validationResult = await dtoValidator.ValidateAsync(@params);
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

#region Validate Refresh Token
public class ValidateRefreshTokenServiceParameters
{
    public Tuser User { get; }

    public RefreshTokenRequestDto Request { get; }

    public ValidateRefreshTokenServiceParameters(Tuser user, RefreshTokenRequestDto request)
    {
        User = user;
        Request = request;
    }
}

public interface IValidateRefreshTokenService : IServiceHandlerAsync<ValidateRefreshTokenServiceParameters, ClaimsPrincipal>
{

}

[ScopedService(typeof(IValidateRefreshTokenService))]
public sealed class ValidateRefreshTokenService : IValidateRefreshTokenService
{
    private readonly IJwtTokenService jwtTokenService;
    private readonly IOptions<JwtAppSetting> options;

    public ValidateRefreshTokenService(IJwtTokenService jwtTokenService, IOptions<JwtAppSetting> options)
    {
        this.jwtTokenService = jwtTokenService;
        this.options = options;
    }

    async Task<Result<ClaimsPrincipal>> IServiceHandlerAsync<ValidateRefreshTokenServiceParameters, ClaimsPrincipal>.HandleAsync(ValidateRefreshTokenServiceParameters @params)
    {
       try
        {
            if(@params is null)
                return ResultExceptionFactory.Error<ClaimsPrincipal>($"{nameof(ValidateRefreshTokenServiceParameters)} is null", HttpStatusCode.BadRequest);

            if(@params.Request is null)
                return ResultExceptionFactory.Error<ClaimsPrincipal>($"{nameof(RefreshTokenRequestDto)} is null", HttpStatusCode.BadRequest);

            RefreshTokenRequestDto refreshTokenRequestDto=@params.Request;

            if(@params.User is null)
                return ResultExceptionFactory.Error<ClaimsPrincipal>($"{nameof(Tuser)} is null", HttpStatusCode.BadRequest);

            Tuser tuser = @params.User;

            // Get Old Access Token and Refresh Token
            string accessToken = refreshTokenRequestDto.AccessToken!;
            string refreshToken = refreshTokenRequestDto.RefreshToken!;

            // Get Principle Claim by passing Old Access Token
            var principal = await jwtTokenService.GetPrincipalFromExpiredTokenAsync(this.options.Value.SecretKey!, accessToken);

            // Get User Identifier
            var identifier = principal.Claims.First(i => i.Type == ClaimTypes.NameIdentifier).Value;

            if(string.IsNullOrEmpty(identifier))
                return ResultExceptionFactory.Error<ClaimsPrincipal>($"Unauthorized access", HttpStatusCode.Unauthorized);

            // Check user Identifier with User Id
            if (Guid.Parse(identifier) != tuser.Identifier)
                return ResultExceptionFactory.Error<ClaimsPrincipal>($"Unauthorized access", HttpStatusCode.Unauthorized);   

            // Check Refresh Token
            if (tuser.TuserToken.RefreshToken != refreshToken || tuser.TuserToken.RefreshTokenExpirayTime < DateTime.UtcNow)
                return ResultExceptionFactory.Error<ClaimsPrincipal>($"Unauthorized access", HttpStatusCode.Unauthorized);

            return Result.Ok(principal);
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error<ClaimsPrincipal>(ex.Message, HttpStatusCode.InternalServerError);
        }
    }

    
}
#endregion 

#region Generate new Token & Refresh Token
public class GenerateTokenServiceParameters
{
    public ClaimsPrincipal Principal { get; }

    public GenerateTokenServiceParameters(ClaimsPrincipal principal)
    {
        Principal = principal;
    }
}

public class GenerateTokenResult
{
    public string AccessToken { get; }
    public string RefreshToken { get; } 

    public GenerateTokenResult(string accessToken, string refreshToken)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
    }
}

public interface IGenerateTokenService : IServiceHandlerAsync<GenerateTokenServiceParameters, GenerateTokenResult>
{

}

[ScopedService(typeof(IGenerateTokenService))]
public sealed class GenerateTokenService : IGenerateTokenService
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IOptions<JwtAppSetting> options;

    public GenerateTokenService(IJwtTokenService jwtTokenService, IOptions<JwtAppSetting> options)
    {
        _jwtTokenService = jwtTokenService;
        this.options = options;
    }

    async Task<Result<GenerateTokenResult>> IServiceHandlerAsync<GenerateTokenServiceParameters, GenerateTokenResult>.HandleAsync(GenerateTokenServiceParameters @params)
    {
        try
        {
            if(@params is null)
                return ResultExceptionFactory.Error<GenerateTokenResult>($"{nameof(GenerateTokenServiceParameters)} is null", HttpStatusCode.BadRequest);

            if(@params.Principal is null)
                return ResultExceptionFactory.Error<GenerateTokenResult>($"{nameof(ClaimsPrincipal)} is null", HttpStatusCode.BadRequest);

            ClaimsPrincipal principal = @params.Principal;

            var newAccessTokenTask = _jwtTokenService.GenerateJwtTokenAsync(this.options.Value, principal.Claims.ToArray(), DateTime.Now.AddDays(1));
            var newRefreshTokenTask = _jwtTokenService.GenerateRefreshTokenAsync();

            await Task.WhenAll(newAccessTokenTask, newRefreshTokenTask);

            GenerateTokenResult generateTokenResult=new GenerateTokenResult(newAccessTokenTask.Result, newRefreshTokenTask.Result);

            return Result.Ok(generateTokenResult);  
        }
        catch(Exception ex)
        {
            return ResultExceptionFactory.Error<GenerateTokenResult>(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}
#endregion 

#region Domain Event Handler
public class RefreshTokenUpdatedDomainEvent : INotification
{
    public Guid? Identifier { get; }

    public StatusEnum Status { get; }

    public RefreshTokenUpdatedDomainEvent(Guid? identifier, StatusEnum status)
    {
        Identifier = identifier;
        Status = status;
    }
}

public class RefreshTokenUpdatedDomainEventHandler : INotificationHandler<RefreshTokenUpdatedDomainEvent>
{
    private readonly ILogger<RefreshTokenUpdatedDomainEventHandler> _logger = null;
    private readonly IUserSharedCacheService _userSharedCacheService = null;


    public RefreshTokenUpdatedDomainEventHandler(ILogger<RefreshTokenUpdatedDomainEventHandler> logger, IUserSharedCacheService userSharedCacheService)
    {
        _logger = logger;
        _userSharedCacheService = userSharedCacheService;
    }

    public async Task HandleCacheAsync(RefreshTokenUpdatedDomainEvent notification, CancellationToken cancellationToken)
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

    Task INotificationHandler<RefreshTokenUpdatedDomainEvent>.Handle(RefreshTokenUpdatedDomainEvent notification, CancellationToken cancellationToken)
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

#region Response Service
public class RefreshTokenResponseServiceParameters
{
    public GenerateTokenResult TokenResult { get; }

    public string AesKey { get; }

    public RefreshTokenResponseServiceParameters(GenerateTokenResult tokenResult, string aesKey)
    {
        TokenResult = tokenResult;
        AesKey = aesKey;
    }
}

public interface IRefreshTokenResponseService : IServiceHandlerAsync<RefreshTokenResponseServiceParameters, AesResponseDto>
{ 

}

[ScopedService(typeof(IRefreshTokenResponseService))]
public sealed class RefreshTokenResponseService : IRefreshTokenResponseService
{
    async Task<Result<AesResponseDto>> IServiceHandlerAsync<RefreshTokenResponseServiceParameters, AesResponseDto>.HandleAsync(RefreshTokenResponseServiceParameters @params)
    {
        try
        {
            if(@params is null)
                return ResultExceptionFactory.Error<AesResponseDto>($"{nameof(RefreshTokenResponseServiceParameters)} is null", HttpStatusCode.BadRequest);

            if(@params.AesKey is null)
                return ResultExceptionFactory.Error<AesResponseDto>($"{nameof(@params.AesKey)} is null", HttpStatusCode.BadRequest);

            string aesKey=@params.AesKey;

            if(@params.TokenResult is null)
                return ResultExceptionFactory.Error<AesResponseDto>($"{nameof(GenerateTokenResult)} is null", HttpStatusCode.BadRequest);

            GenerateTokenResult generateTokenResult=@params.TokenResult;

            // Map
            RefreshTokenResponseDto refreshTokenResponseDto = new RefreshTokenResponseDto();
            refreshTokenResponseDto.RefreshToken=generateTokenResult.RefreshToken;
            refreshTokenResponseDto.Token = generateTokenResult.AccessToken;

            // Encrypt Response
            IAesEncrypteWrapper<RefreshTokenResponseDto> aesEncrypteWrapper=new AesEncryptWrapper<RefreshTokenResponseDto>();
            var aesEncryptResult=await aesEncrypteWrapper.HandleAsync(new AesEncrypteWrapperParameter<RefreshTokenResponseDto>(aesKey, refreshTokenResponseDto));
            if (aesEncryptResult.IsFailed)
                return ResultExceptionFactory.Error<AesResponseDto>(aesEncryptResult.Errors[0]);

            // Map Response
            AesResponseDto aesResponseDto = new AesResponseDto();
            aesResponseDto.Body = aesEncryptResult.Value;

            return Result.Ok(aesResponseDto);
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error<AesResponseDto>(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}

#endregion 

#region Command Handler Service
public class RefreshTokenCommand: IRequest<DataResponse<AesResponseDto>>
{
    public AesRequestDto Request { get; }

    public string ClientId { get; }

    public RefreshTokenCommand(AesRequestDto request, string clientId)
    {
        Request = request;
        ClientId = clientId;
    }
}

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, DataResponse<AesResponseDto>>
{

    private readonly IDataResponseFactory _dataResponseFactory;
    private readonly IRefreshTokenDecryptService _refreshTokenDecryptService;
    private readonly IRefreshTokenValidationService _refreshTokenValidationService;
    private readonly IValidateRefreshTokenService _validateRefreshTokenService;
    private readonly IGenerateTokenService _generateTokenService;
    private readonly IUpdateRefreshTokenDbService _updateRefreshTokenDbService;
    private readonly IMediator _medaitor;
    private readonly IRefreshTokenResponseService _refreshTokenResponseService;

    public RefreshTokenCommandHandler(
        IDataResponseFactory dataResponseFactory, 
        IRefreshTokenDecryptService refreshTokenDecryptService,
        IRefreshTokenValidationService refreshTokenValidationService,
        IValidateRefreshTokenService validateRefreshTokenService,
        IGenerateTokenService generateTokenService,
        IUpdateRefreshTokenDbService updateRefreshTokenDbService,
        IMediator mediator,
        IRefreshTokenResponseService refreshTokenResponseService
        )
    {
        _dataResponseFactory = dataResponseFactory;
        _refreshTokenDecryptService = refreshTokenDecryptService;
        _refreshTokenValidationService = refreshTokenValidationService;
        _validateRefreshTokenService = validateRefreshTokenService;
        _generateTokenService = generateTokenService;
        _updateRefreshTokenDbService = updateRefreshTokenDbService;
        _medaitor = mediator;
        _refreshTokenResponseService = refreshTokenResponseService;
    }

    async Task<DataResponse<AesResponseDto>> IRequestHandler<RefreshTokenCommand, DataResponse<AesResponseDto>>
        .Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if(request is null)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>("Request is null", (int)HttpStatusCode.BadRequest);

            var aesRequestDto = request.Request;

            if (aesRequestDto is null)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>("AesRequestDto is null", (int)HttpStatusCode.BadRequest);

            var clientId = request.ClientId;

            if (string.IsNullOrEmpty(clientId))
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>("ClientId is null or empty", (int)HttpStatusCode.BadRequest);

            // Decrypt Request
            var aesDecryptResult=await _refreshTokenDecryptService.HandleAsync(
                new RefreshTokenDecryptServiceParameters(
                    clientId, 
                    aesRequestDto, 
                    cancellationToken
                    )
                );

            if(aesDecryptResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(aesDecryptResult.Errors[0].Message, (int)aesDecryptResult.Errors[0].Metadata[ConstantValue.StatusCode]);

            RefreshTokenDecryptServiceResult refreshTokenDecryptServiceResult = aesDecryptResult.Value;

            RefreshTokenRequestDto refreshTokenRequestDto = refreshTokenDecryptServiceResult.Request;
            Tuser user=refreshTokenDecryptServiceResult.User;

            // Validation
            var validationResult = await _refreshTokenValidationService.HandleAsync(refreshTokenRequestDto);
            if(validationResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(validationResult.Errors[0].Message, (int)validationResult.Errors[0].Metadata[ConstantValue.StatusCode]);

            // Validate Refresh Token
            var validateRefreshTokenResult = await _validateRefreshTokenService.HandleAsync(new ValidateRefreshTokenServiceParameters(user, refreshTokenRequestDto));
            if (validateRefreshTokenResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(validateRefreshTokenResult.Errors[0].Message, (int)validateRefreshTokenResult.Errors[0].Metadata[ConstantValue.StatusCode]);

            ClaimsPrincipal claimsPrincipal = validateRefreshTokenResult.Value;

            // Generate new Token and Refresh Token
            var generateTokenResult = await _generateTokenService.HandleAsync(new GenerateTokenServiceParameters(claimsPrincipal));
            if (generateTokenResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(generateTokenResult.Errors[0].Message, (int)generateTokenResult.Errors[0].Metadata[ConstantValue.StatusCode]);

            GenerateTokenResult tokenResult = generateTokenResult.Value;

            // Update Refresh Token
            var updateRefreshTokenResult = await _updateRefreshTokenDbService.HandleAsync(
                new UpdateRefreshTokenDbServiceParameters(
                        user, 
                        tokenResult?.RefreshToken, 
                        cancellationToken
                    )
                );

            if (updateRefreshTokenResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(updateRefreshTokenResult.Errors[0].Message, (int)updateRefreshTokenResult.Errors[0].Metadata[ConstantValue.StatusCode]);

            // Publish Domain Event
            _=_medaitor.Publish(new RefreshTokenUpdatedDomainEvent(user.Identifier,(StatusEnum)Convert.ToInt32(user.Status)), cancellationToken);

            // Encrypt Response
            var aesResponseDtoResult = await _refreshTokenResponseService.HandleAsync(
                new RefreshTokenResponseServiceParameters(tokenResult!,user.TuserCredential.AesSecretKey));

            if (aesResponseDtoResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AesResponseDto>(aesResponseDtoResult.Errors[0].Message, (int)aesResponseDtoResult.Errors[0].Metadata[ConstantValue.StatusCode]);

            return await _dataResponseFactory.SuccessAsync<AesResponseDto>((int)HttpStatusCode.OK, aesResponseDtoResult.Value,"Refresh Token updated");
        }
        catch (Exception ex)
        {
            return await _dataResponseFactory.ErrorAsync<AesResponseDto>(ex.Message, (int)HttpStatusCode.InternalServerError);
        }
    }
}
#endregion 
