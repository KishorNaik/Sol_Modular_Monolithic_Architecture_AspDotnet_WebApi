using FluentResults;
using FluentValidation;
using Models.Shared.Constant;
using Models.Shared.Requests;
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

#endregion Validation Service

#region Decrypt and Validate Service

public class AddOrganizationDecrypteAndValidateParameters
{
    public AesRequestDto? Request { get; }

    public AddOrganizationDecrypteAndValidateParameters(AesRequestDto request)
    {
        Request = request;
    }
}

public interface IAddOrganizationDecrypteAndValidateService : IServiceHandlerAsync<AddOrganizationDecrypteAndValidateParameters, AddOrganizationRequestDto>
{
}

[ScopedService(typeof(IAddOrganizationDecrypteAndValidateService))]
public class AddOrganizationDecrypteAndValidateService : IAddOrganizationDecrypteAndValidateService
{
    private readonly IConfigHelper _configHelper;

    public AddOrganizationDecrypteAndValidateService(IConfigHelper configHelper)
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

            // Decrypt and Validate
            AesDecryptAndValidation aesDecryptAndValidation = new AesDecryptAndValidation(aesSecret.Value);
            var aesDecryptionAndValidationResult = await aesDecryptAndValidation.WrapperAsync<AddOrganizationRequestDto, AddOrganizationValidator>(aesRequestDto);

            if (aesDecryptionAndValidationResult.IsFailed)
                return ResultExceptionFactory.Error<AddOrganizationRequestDto>(aesDecryptionAndValidationResult.Errors[0]);

            return Result.Ok(aesDecryptionAndValidationResult.Value);
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error<AddOrganizationRequestDto>(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}

#endregion Decrypt and Validate Service

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
                Name = @params.Name,
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

#region Add Organization Response Service

public interface IAddOrganizationResponseService : IServiceHandlerAsync<Torganization, AddOrganizationResponseDto>
{
}

[ScopedService(typeof(IAddOrganizationResponseService))]
public class AddOrganizationResponseService : IAddOrganizationResponseService
{
    async Task<Result<AddOrganizationResponseDto>> IServiceHandlerAsync<Torganization, AddOrganizationResponseDto>.HandleAsync(Torganization @params)
    {
        try
        {
            if (@params is null)
                return ResultExceptionFactory.Error<AddOrganizationResponseDto>($"{nameof(Torganization)} object is null", HttpStatusCode.BadRequest);

            AddOrganizationResponseDto addOrganizationResponseDto = new AddOrganizationResponseDto
            {
                Identifier = @params.Identifier,
            };

            return Result.Ok(addOrganizationResponseDto);
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error<AddOrganizationResponseDto>(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}

#endregion Add Organization Response Service

#region Command Handler

public class AddOrganizationCommand : IRequest<DataResponse<AddOrganizationResponseDto>>
{
    public AesRequestDto? Request { get; set; }

    public AddOrganizationCommand(AesRequestDto request)
    {
        Request = request;
    }
}

public class AddOrganizationCommandHandler : IRequestHandler<AddOrganizationCommand, DataResponse<AddOrganizationResponseDto>>
{
    private readonly IDataResponseFactory _dataResponseFactory;
    private readonly IAddOrganizationDecrypteAndValidateService _addOrganizationDecrypteAndValidateService;
    private readonly IAddOrganizationRequestEntityMapService _addOrganizationRequestEntityMapService;
    private readonly IAddOrganizationDbService _addOrganizationDbService;
    private readonly IAddOrganizationResponseService _addOrganizationResponseService;

    public AddOrganizationCommandHandler(
        IDataResponseFactory dataResponseFactory,
        IAddOrganizationDecrypteAndValidateService addOrganizationDecrypteAndValidateService,
        IAddOrganizationRequestEntityMapService addOrganizationRequestEntityMapService,
        IAddOrganizationDbService addOrganizationDbService,
        IAddOrganizationResponseService addOrganizationResponseService
        )
    {
        _dataResponseFactory = dataResponseFactory;
        _addOrganizationDecrypteAndValidateService = addOrganizationDecrypteAndValidateService;
        _addOrganizationRequestEntityMapService = addOrganizationRequestEntityMapService;
        _addOrganizationDbService = addOrganizationDbService;
        _addOrganizationResponseService = addOrganizationResponseService;
    }

    async Task<DataResponse<AddOrganizationResponseDto>> IRequestHandler<AddOrganizationCommand, DataResponse<AddOrganizationResponseDto>>.Handle(AddOrganizationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request is null)
                return await _dataResponseFactory.ErrorAsync<AddOrganizationResponseDto>($"{nameof(AddOrganizationCommand)} object is null", (int)HttpStatusCode.BadRequest);

            AesRequestDto aesRequestDto = request.Request!;
            if (aesRequestDto is null)
                return await _dataResponseFactory.ErrorAsync<AddOrganizationResponseDto>("Request object is null", (int)HttpStatusCode.BadRequest);

            // Decrypt and Validate
            var aesDecryptionAndValidationResult = await _addOrganizationDecrypteAndValidateService.HandleAsync(new AddOrganizationDecrypteAndValidateParameters(aesRequestDto));
            if (aesDecryptionAndValidationResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AddOrganizationResponseDto>(aesDecryptionAndValidationResult.Errors[0].Message, (int)HttpStatusCode.BadRequest);

            AddOrganizationRequestDto addOrganizationRequestDto = aesDecryptionAndValidationResult.Value!;

            // Map AddOraganizationRequestDTO to TOrganization Entity
            var organizationResult = await _addOrganizationRequestEntityMapService.HandleAsync(addOrganizationRequestDto);
            if (organizationResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AddOrganizationResponseDto>(organizationResult.Errors[0].Message, (int)HttpStatusCode.BadRequest);

            Torganization torganization = organizationResult.Value!;

            // Add Organization
            var addOrganizationResult = await _addOrganizationDbService.HandleAsync(new AddOrganizationSqlParameters(torganization, cancellationToken));
            if (addOrganizationResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AddOrganizationResponseDto>(addOrganizationResult.Errors[0].Message, (int)HttpStatusCode.BadRequest);

            torganization = addOrganizationResult.Value!;

            // Response
            var responseResult = await _addOrganizationResponseService.HandleAsync(torganization);
            if (responseResult.IsFailed)
                return await _dataResponseFactory.ErrorAsync<AddOrganizationResponseDto>(responseResult.Errors[0].Message, (int)HttpStatusCode.BadRequest);

            return await _dataResponseFactory.SuccessAsync<AddOrganizationResponseDto>((int)HttpStatusCode.Created, responseResult.Value, "Organization added successfully");
        }
        catch (Exception ex)
        {
            return await _dataResponseFactory.ErrorAsync<AddOrganizationResponseDto>(ex.Message, (int)HttpStatusCode.InternalServerError);
        }
    }
}

#endregion Command Handler