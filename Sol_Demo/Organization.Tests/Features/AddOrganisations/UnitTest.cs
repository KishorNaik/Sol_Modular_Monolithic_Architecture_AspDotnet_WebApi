using FluentResults;
using MediatR;
using Models.Shared.Requests;
using Models.Shared.Responses;
using Moq;
using Organization.Applications.Features.v1.AddOrganization;
using Organization.Contracts.Features.AddOrganizations;
using Organization.Infrastructures.Entities;
using Organization.Infrastructures.Services.AddOrganization;
using System.Net;
using Utility.Shared.Exceptions;
using Utility.Shared.Response;

namespace Organization.Tests.Features.AddOrganisations;

public class AddOrganisationUnitTests
{
    private readonly Mock<IDataResponseFactory> _mockDataResponseFactory;
    private readonly Mock<IAddOrganizationDecrypteService> _mockDecrypteService;
    private readonly Mock<IAddOrgnizationValidationService> _mockValidationService;
    private readonly Mock<IAddOrganizationRequestEntityMapService> _mockRequestEntityMapService;
    private readonly Mock<IAddOrganizationDbService> _mockDbService;
    private readonly Mock<IAddOrganizationResponseService> _mockResponseService;
    private readonly IRequestHandler<AddOrganizationCommand, DataResponse<AddOrganizationResponseDto>> _handler;

    public AddOrganisationUnitTests()
    {
        _mockDataResponseFactory = new Mock<IDataResponseFactory>();
        _mockDecrypteService = new Mock<IAddOrganizationDecrypteService>();
        _mockValidationService = new Mock<IAddOrgnizationValidationService>();
        _mockRequestEntityMapService = new Mock<IAddOrganizationRequestEntityMapService>();
        _mockDbService = new Mock<IAddOrganizationDbService>();
        _mockResponseService = new Mock<IAddOrganizationResponseService>();

        _handler = new AddOrganizationCommandHandler(
           _mockDataResponseFactory.Object,
           _mockDecrypteService.Object,
           _mockValidationService.Object,
           _mockRequestEntityMapService.Object,
           _mockDbService.Object,
           _mockResponseService.Object
       );
    }

    [Fact]
    public async Task Should_Return_Null_When_Command_Object_Is_Null()
    {
        // Arrange
        AddOrganizationCommand command = null;

        _mockDataResponseFactory
           .Setup(factory => factory.ErrorAsync<AddOrganizationResponseDto>($"{nameof(AddOrganizationCommand)} object is null", Convert.ToInt32(HttpStatusCode.BadRequest), null!))
           .ReturnsAsync(new DataResponse<AddOrganizationResponseDto> { Success = false, StatusCode = (int)HttpStatusCode.BadRequest });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Should_Return_Null_When_AesRequestDto_Object_Is_Null()
    {
        // Arrange
        var command = new AddOrganizationCommand(null!);

        _mockDataResponseFactory
           .Setup(factory => factory.ErrorAsync<AddOrganizationResponseDto>("Request object is null", Convert.ToInt32(HttpStatusCode.BadRequest), null!))
           .ReturnsAsync(new DataResponse<AddOrganizationResponseDto> { Success = false, StatusCode = (int)HttpStatusCode.BadRequest });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Should_Return_Failed_When_Decrypte_Is_Failed()
    {
        // Arrange
        var aesRequestDto = new AesRequestDto();
        var command = new AddOrganizationCommand(aesRequestDto);

        _mockDecrypteService
            .Setup((service) => service.HandleAsync(It.IsAny<AddOrganizationDecrypteAndValidateParameters>()))
            .ReturnsAsync(ResultExceptionFactory.Error<AddOrganizationRequestDto>("Failed", HttpStatusCode.BadRequest));

        _mockDataResponseFactory
          .Setup(factory => factory.ErrorAsync<AddOrganizationResponseDto>("Failed", Convert.ToInt32(HttpStatusCode.BadRequest), null!))
          .ReturnsAsync(new DataResponse<AddOrganizationResponseDto> { Success = false, StatusCode = (int)HttpStatusCode.BadRequest });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Should_Return_Failed_When_Validation_Is_Failed()
    {
        // Arrange
        var aesRequestDto = new AesRequestDto();
        var command = new AddOrganizationCommand(aesRequestDto);

        var addOrganizationRequestDto = new AddOrganizationRequestDto();

        _mockDecrypteService
          .Setup((service) => service.HandleAsync(It.IsAny<AddOrganizationDecrypteAndValidateParameters>()))
          .ReturnsAsync(Result.Ok(addOrganizationRequestDto));

        _mockValidationService
            .Setup((service) => service.HandleAsync(It.IsAny<AddOrganizationRequestDto>()))
            .ReturnsAsync(ResultExceptionFactory.Error("Failed", HttpStatusCode.BadRequest));

        _mockDataResponseFactory
          .Setup(factory => factory.ErrorAsync<AddOrganizationResponseDto>("Failed", Convert.ToInt32(HttpStatusCode.BadRequest), null!))
          .ReturnsAsync(new DataResponse<AddOrganizationResponseDto> { Success = false, StatusCode = (int)HttpStatusCode.BadRequest });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Should_Return_Failed_When_Mapping_Entity_Failed()
    {
        // Arrange
        var aesRequestDto = new AesRequestDto();
        var command = new AddOrganizationCommand(aesRequestDto);

        var addOrganizationRequestDto = new AddOrganizationRequestDto();

        _mockDecrypteService
            .Setup((service) => service.HandleAsync(It.IsAny<AddOrganizationDecrypteAndValidateParameters>()))
            .ReturnsAsync(Result.Ok(addOrganizationRequestDto));

        _mockValidationService
           .Setup((service) => service.HandleAsync(It.IsAny<AddOrganizationRequestDto>()))
           .ReturnsAsync(Result.Ok());

        _mockRequestEntityMapService
            .Setup((service) => service.HandleAsync(addOrganizationRequestDto))
            .ReturnsAsync(ResultExceptionFactory.Error<Torganization>("Failed", HttpStatusCode.BadRequest));

        _mockDataResponseFactory
          .Setup(factory => factory.ErrorAsync<AddOrganizationResponseDto>("Failed", Convert.ToInt32(HttpStatusCode.BadRequest), null!))
          .ReturnsAsync(new DataResponse<AddOrganizationResponseDto> { Success = false, StatusCode = (int)HttpStatusCode.BadRequest });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Should_Return_Failed_When_Add_Organization_Insert_Data_Failed()
    {
        // Arrange
        var aesRequestDto = new AesRequestDto();
        var command = new AddOrganizationCommand(aesRequestDto);

        var addOrganizationRequestDto = new AddOrganizationRequestDto();
        var torganization = new Torganization();

        _mockDecrypteService
            .Setup((service) => service.HandleAsync(It.IsAny<AddOrganizationDecrypteAndValidateParameters>()))
            .ReturnsAsync(Result.Ok(addOrganizationRequestDto));

        _mockValidationService
          .Setup((service) => service.HandleAsync(It.IsAny<AddOrganizationRequestDto>()))
          .ReturnsAsync(Result.Ok());

        _mockRequestEntityMapService
            .Setup((service) => service.HandleAsync(addOrganizationRequestDto))
            .ReturnsAsync(Result.Ok(torganization));

        _mockDbService
            .Setup((service) => service.HandleAsync(It.IsAny<AddOrganizationSqlParameters>()))
            .ReturnsAsync(ResultExceptionFactory.Error<Torganization>("Failed", HttpStatusCode.BadRequest));

        _mockDataResponseFactory
          .Setup(factory => factory.ErrorAsync<AddOrganizationResponseDto>("Failed", Convert.ToInt32(HttpStatusCode.BadRequest), null!))
          .ReturnsAsync(new DataResponse<AddOrganizationResponseDto> { Success = false, StatusCode = (int)HttpStatusCode.BadRequest });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Should_Return_Failed_When_Add_Organization_Response_Failed()
    {
        // Arrange
        var aesRequestDto = new AesRequestDto();
        var command = new AddOrganizationCommand(aesRequestDto);

        var addOrganizationRequestDto = new AddOrganizationRequestDto();
        var torganization = new Torganization();

        _mockDecrypteService
            .Setup((service) => service.HandleAsync(It.IsAny<AddOrganizationDecrypteAndValidateParameters>()))
            .ReturnsAsync(Result.Ok(addOrganizationRequestDto));

        _mockValidationService
          .Setup((service) => service.HandleAsync(It.IsAny<AddOrganizationRequestDto>()))
          .ReturnsAsync(Result.Ok());

        _mockRequestEntityMapService
            .Setup((service) => service.HandleAsync(addOrganizationRequestDto))
            .ReturnsAsync(Result.Ok(torganization));

        _mockDbService
            .Setup((service) => service.HandleAsync(It.IsAny<AddOrganizationSqlParameters>()))
            .ReturnsAsync(Result.Ok(torganization));

        _mockResponseService
            .Setup((service) => service.HandleAsync(torganization))
            .ReturnsAsync(ResultExceptionFactory.Error<AddOrganizationResponseDto>("Failed", HttpStatusCode.BadRequest));

        _mockDataResponseFactory
          .Setup(factory => factory.ErrorAsync<AddOrganizationResponseDto>("Failed", Convert.ToInt32(HttpStatusCode.BadRequest), null!))
          .ReturnsAsync(new DataResponse<AddOrganizationResponseDto> { Success = false, StatusCode = (int)HttpStatusCode.BadRequest });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Should_Return_Success_When_Add_Organization_Response_Success()
    {
        // Arrange
        var aesRequestDto = new AesRequestDto();
        var command = new AddOrganizationCommand(aesRequestDto);

        var addOrganizationRequestDto = new AddOrganizationRequestDto();
        var torganization = new Torganization();
        var addOrganizationResponseDto = new AddOrganizationResponseDto();

        _mockDecrypteService
            .Setup((service) => service.HandleAsync(It.IsAny<AddOrganizationDecrypteAndValidateParameters>()))
            .ReturnsAsync(Result.Ok(addOrganizationRequestDto));

        _mockValidationService
          .Setup((service) => service.HandleAsync(It.IsAny<AddOrganizationRequestDto>()))
          .ReturnsAsync(Result.Ok());

        _mockRequestEntityMapService
            .Setup((service) => service.HandleAsync(addOrganizationRequestDto))
            .ReturnsAsync(Result.Ok(torganization));

        _mockDbService
            .Setup((service) => service.HandleAsync(It.IsAny<AddOrganizationSqlParameters>()))
            .ReturnsAsync(Result.Ok(torganization));

        _mockResponseService
            .Setup((service) => service.HandleAsync(torganization))
            .ReturnsAsync(Result.Ok(addOrganizationResponseDto));

        _mockDataResponseFactory
          .Setup(factory => factory.SuccessAsync<AddOrganizationResponseDto>((int)HttpStatusCode.Created, addOrganizationResponseDto, "Organization added successfully"))
          .ReturnsAsync(new DataResponse<AddOrganizationResponseDto> { Success = true, StatusCode = (int)HttpStatusCode.Created });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal((int)HttpStatusCode.Created, result.StatusCode);
    }
}