
using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Models.Shared.Enums;
using sorovi.DependencyInjection.AutoRegister;
using System.Net;
using Users.Infrastructures.Contexts;
using Utility.Shared.Exceptions;
using Utility.Shared.ServiceHandler;

namespace Users.Infrastructures.Services.GetUsersByIdentifier;

#region Db Service
public class GetUserByIdentiferDbServiceSqlParameters
{
    public Guid? Identifer { get; }

    public StatusEnum? Status { get; }

    public CancellationToken CancellationToken { get; }

    public GetUserByIdentiferDbServiceSqlParameters(Guid? identifer, StatusEnum? status, CancellationToken cancellationToken)
    {
        Identifer = identifer;
        Status = status;
        CancellationToken = cancellationToken;
    }
}

public class UserResult
{
   
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public UserTypeEnum UserType { get; set; }

    public StatusEnum Status { get; set; }
}

public class UserCommunicationResult
{
    public string? EmailId { get; set; }

    public string? MobileNumber { get; set; }
}

public class UserCredentailsResult
{
    public string? Salt { get; set; }

    public string? Hash { get; set; }

    public Guid? ClientId { get; set; }

    public string? AesSecretKey { get; set; }

    public string? HmacSecretKey { get; set; }

}

public class UserSettingsResult
{
    public bool? IsEmailVerified { get; set; }
}

public class UserTokens
{
    public Guid? EmailToken { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiry { get; set; }

    public Guid? PasswordResetToken { get; set; }
}

public class UserOrganizationResult
{
    public Guid? OrgId { get; set; }
}


public class GetUserByIdentiferDbServiceResult
{
    public Guid? Identifier { get; set; }

    public UserResult? User { get; set; }

    public UserCommunicationResult? UserCommunication { get; set; }

    public UserCredentailsResult? UserCredentials { get; set; }

    public UserSettingsResult? UserSettings { get; set; }

    public UserTokens? UserTokens { get; set; }

    public UserOrganizationResult? UserOrganization { get; set; }

    public byte[]? Version { get; set; }
}

public interface IGetUserByIdentiferDbService : IServiceHandlerAsync<GetUserByIdentiferDbServiceSqlParameters, GetUserByIdentiferDbServiceResult>
{
   
}

[ScopedService(typeof(IGetUserByIdentiferDbService))]
public sealed class GetUserByIdentiferDbService : IGetUserByIdentiferDbService
{
    private readonly UsersDbContext _usersDbContext;

    public GetUserByIdentiferDbService(UsersDbContext usersDbContext)
    {
      _usersDbContext = usersDbContext;
    }

    async Task<Result<GetUserByIdentiferDbServiceResult>> IServiceHandlerAsync<GetUserByIdentiferDbServiceSqlParameters, GetUserByIdentiferDbServiceResult>
        .HandleAsync(GetUserByIdentiferDbServiceSqlParameters @params)
    {
        try
        { 
            if(@params is null)
                return ResultExceptionFactory.Error<GetUserByIdentiferDbServiceResult>($"{nameof(GetUserByIdentiferDbServiceSqlParameters)} is null", httpStatusCode: HttpStatusCode.BadRequest);

            if(@params.Identifer is null)
                return ResultExceptionFactory.Error<GetUserByIdentiferDbServiceResult>($"{nameof(@params.Identifer)} is null", httpStatusCode: HttpStatusCode.BadRequest);

            var result = await _usersDbContext
                .Tusers
                .AsNoTracking()
                .AsQueryable()
                .Where(x => x.Identifier == @params.Identifer && x.Status == Convert.ToBoolean((int)@params.Status!))
                .Select(x => new GetUserByIdentiferDbServiceResult()
                {
                    Identifier = x.Identifier,
                    Version=x.Version,
                    User = new UserResult()
                    {
                        FirstName = x.FirstName,
                        LastName = x.LastName,
                        UserType = (UserTypeEnum)x.UserType,
                        Status = (StatusEnum)Convert.ToInt32(x.Status),
                    },
                    UserCommunication = new UserCommunicationResult()
                    {
                        EmailId = x.TuserCommunication.EmailId,
                        MobileNumber = x.TuserCommunication.MobileNumber
                    },
                    UserCredentials = new UserCredentailsResult()
                    {
                        Salt = x.TuserCredential.Salt,
                        Hash = x.TuserCredential.Hash,
                        ClientId = x.TuserCredential.ClientId,
                        AesSecretKey = x.TuserCredential.AesSecretKey,
                        HmacSecretKey = x.TuserCredential.HmacSecretKey
                    },
                    UserSettings = new UserSettingsResult()
                    {
                        IsEmailVerified = x.TuserSetting.IsEmailVerified
                    },
                    UserTokens = new UserTokens()
                    {
                        EmailToken = x.TuserToken.EmailToken,
                        RefreshToken = x.TuserToken.RefreshToken,
                        RefreshTokenExpiry = x.TuserToken.RefreshTokenExpirayTime,
                        PasswordResetToken = x.TuserToken.PasswordResetToken
                    },
                    UserOrganization = new UserOrganizationResult()
                    {
                        OrgId = x.TusersOrganization.OrgId
                    }
                })
                .FirstOrDefaultAsync(@params.CancellationToken);

            if(result is null)
                return ResultExceptionFactory.Error<GetUserByIdentiferDbServiceResult>($"{nameof(@params.Identifer)} is not found", httpStatusCode: HttpStatusCode.NotFound);

            return Result.Ok(result);

        }
        catch(Exception ex)
        {
            return ResultExceptionFactory.Error(ex.Message, httpStatusCode: HttpStatusCode.InternalServerError);
        }
    }
}

#endregion 
