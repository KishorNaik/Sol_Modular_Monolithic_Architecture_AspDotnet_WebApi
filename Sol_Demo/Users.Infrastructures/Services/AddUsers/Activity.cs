
using FluentResults;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using sorovi.DependencyInjection.AutoRegister;
using System.Net;
using Users.Infrastructures.Contexts;
using Users.Infrastructures.Entities;
using Utility.Shared.Exceptions;
using Utility.Shared.ServiceHandler;

namespace Users.Infrastructures.Services.AddUsers;


#region Add User Db Service

public class AddUserDbServiceSqlParameters
{
    public CancellationToken CancellationToken { get;}

    public Tuser? User { get; }

    public AddUserDbServiceSqlParameters(Tuser? user, CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
        User = user;
    }

}

public interface IAddUserDbService : IServiceHandlerAsync<AddUserDbServiceSqlParameters, Tuser>
{

}

[ScopedService(typeof(IAddUserDbService))]
public sealed class AddUserDbService : IAddUserDbService
{
    private readonly UsersDbContext _userContext;

    public AddUserDbService(UsersDbContext userContext)
    {
        _userContext = userContext;
    }
   
    async Task<Result<Tuser>> IServiceHandlerAsync<AddUserDbServiceSqlParameters, Tuser>.HandleAsync(AddUserDbServiceSqlParameters @params)
    {
        try
        { 
            if(@params is null)
                return ResultExceptionFactory.Error<Tuser>($"{nameof(AddUserDbServiceSqlParameters)} is null", httpStatusCode: HttpStatusCode.BadRequest);

            if(@params.User is null)
                return ResultExceptionFactory.Error<Tuser>($"{nameof(Tuser)} is null", httpStatusCode: HttpStatusCode.BadRequest);

            Tuser tuser = @params.User;

            // Add Users
            await _userContext.Tusers.AddAsync(tuser, @params.CancellationToken);
            await _userContext.SaveChangesAsync(@params.CancellationToken);

            return Result.Ok(tuser);
        }
        catch (Exception ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
        {
            return ResultExceptionFactory.Error<Tuser>("User already exists", httpStatusCode: HttpStatusCode.Conflict);
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error<Tuser>(ex.Message, httpStatusCode: HttpStatusCode.InternalServerError);
        }
    }
}

#endregion
/*
#region Add User Communication Db Service
public class AddUserCommunicationSqlParameters
{
    public UsersDbContext? UsersDbContext { get; }

    public TuserCommunication? UserCommunication { get; }

    public CancellationToken CancellationToken { get; }

    public AddUserCommunicationSqlParameters(UsersDbContext? usersDbContext, TuserCommunication? userCommunication, CancellationToken cancellationToken)
    {
        UsersDbContext = usersDbContext;
        UserCommunication = userCommunication;
        CancellationToken = cancellationToken;
    }
}

public interface IAddUserCommunicationDbService : IServiceHandlerAsync<AddUserCommunicationSqlParameters, bool>
{

}

[ScopedService(typeof(IAddUserCommunicationDbService))]
public sealed class AddUserCommunicationDbService : IAddUserCommunicationDbService
{
    async Task<Result<bool>> IServiceHandlerAsync<AddUserCommunicationSqlParameters, bool>.HandleAsync(AddUserCommunicationSqlParameters @params)
    {
        try
        {
            if(@params.UsersDbContext is null)
                return ResultExceptionFactory.Error<bool>($"{nameof(UsersDbContext)} is null", httpStatusCode: HttpStatusCode.BadRequest);

            if(@params.UserCommunication is null)
                return ResultExceptionFactory.Error<bool>($"{nameof(TuserCommunication)} is null", httpStatusCode: HttpStatusCode.BadRequest);

            TuserCommunication tuserCommunication = @params.UserCommunication;

            // Add User Communication
            await @params.UsersDbContext.AddAsync(tuserCommunication, @params.CancellationToken);
            int result=await @params.UsersDbContext.SaveChangesAsync(@params.CancellationToken);

            if(result<=0)
                return ResultExceptionFactory.Error<bool>("User Communication not added", httpStatusCode: HttpStatusCode.Conflict);
            

            return Result.Ok(true);
        }
        catch (Exception ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
        {
            return ResultExceptionFactory.Error<bool>("User already exists", httpStatusCode: HttpStatusCode.Conflict);
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error<bool>(ex.Message, httpStatusCode: HttpStatusCode.InternalServerError);
        }

    }
}

#endregion

#region Add User Credentials Db Service
public class AddUserCredentialsSqlParameters
{
    public UsersDbContext? UsersDbContext { get; }

    public TuserCredential? UserCredentials { get; }

    public CancellationToken CancellationToken { get; }

    public AddUserCredentialsSqlParameters(UsersDbContext? usersDbContext, TuserCredential? userCredentials, CancellationToken cancellationToken)
    {
        UsersDbContext = usersDbContext;
        UserCredentials = userCredentials;
        CancellationToken = cancellationToken;
    }
}

public interface IAddUserCredentialsDbService : IServiceHandlerAsync<AddUserCredentialsSqlParameters, bool>
{ }

[ScopedService(typeof(IAddUserCredentialsDbService))]
public sealed class AddUserCredentialsDbService : IAddUserCredentialsDbService
{
    async Task<Result<bool>> IServiceHandlerAsync<AddUserCredentialsSqlParameters, bool>.HandleAsync(AddUserCredentialsSqlParameters @params)
    {
        try
        {
            if (@params.UsersDbContext is null)
                return ResultExceptionFactory.Error<bool>($"{nameof(UsersDbContext)} is null", httpStatusCode: HttpStatusCode.BadRequest);

            if (@params.UserCredentials is null)
                return ResultExceptionFactory.Error<bool>($"{nameof(TuserCredential)} is null", httpStatusCode: HttpStatusCode.BadRequest);

            TuserCredential userCredentials = @params.UserCredentials;

            // Add User Communication
            await @params.UsersDbContext.AddAsync(userCredentials, @params.CancellationToken);
            int result = await @params.UsersDbContext.SaveChangesAsync(@params.CancellationToken);

            if (result <= 0)
                return ResultExceptionFactory.Error<bool>("User Credentials not added", httpStatusCode: HttpStatusCode.Conflict);


            return Result.Ok(true);
        }
        catch (Exception ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
        {
            return ResultExceptionFactory.Error<bool>("User already exists", httpStatusCode: HttpStatusCode.Conflict);
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error<bool>(ex.Message, httpStatusCode: HttpStatusCode.InternalServerError);
        }
    }
}
#endregion

#region Add Token Service Db Service
public class AddUserTokenSqlParameters
{
    public UsersDbContext? UsersDbContext { get; }

    public TuserToken? UserToken { get; }

    public CancellationToken CancellationToken { get; }

    public AddUserTokenSqlParameters(UsersDbContext? usersDbContext, TuserToken? userToken, CancellationToken cancellationToken)
    {
        UsersDbContext = usersDbContext;
        UserToken = userToken;
        CancellationToken = cancellationToken;
    }
}

public interface IAddUserTokenDbService : IServiceHandlerAsync<AddUserTokenSqlParameters, bool>
{ }

[ScopedService(typeof(IAddUserTokenDbService))]
public sealed class AddUserTokenDbService : IAddUserTokenDbService
{
    async Task<Result<bool>> IServiceHandlerAsync<AddUserTokenSqlParameters, bool>.HandleAsync(AddUserTokenSqlParameters @params)
    {
        try
        {
            if (@params.UsersDbContext is null)
                return ResultExceptionFactory.Error<bool>($"{nameof(UsersDbContext)} is null", httpStatusCode: HttpStatusCode.BadRequest);

            if (@params.UserToken is null)
                return ResultExceptionFactory.Error<bool>($"{nameof(TuserToken)} is null", httpStatusCode: HttpStatusCode.BadRequest);

            TuserToken userToken = @params.UserToken;

            // Add User Communication
            await @params.UsersDbContext.AddAsync(userToken, @params.CancellationToken);
            int result = await @params.UsersDbContext.SaveChangesAsync(@params.CancellationToken);

            if (result <= 0)
                return ResultExceptionFactory.Error<bool>("User Token not added", httpStatusCode: HttpStatusCode.Conflict);


            return Result.Ok(true);
        }
        catch (Exception ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
        {
            return ResultExceptionFactory.Error<bool>("User already exists", httpStatusCode: HttpStatusCode.Conflict);
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error<bool>(ex.Message, httpStatusCode: HttpStatusCode.InternalServerError);
        }
    }
}
#endregion

#region Add User Settings Db Service
public class AddUserSettingsSqlParameters
{
    public UsersDbContext? UsersDbContext { get; }

    public TuserSetting? UserSettings { get; }

    public CancellationToken CancellationToken { get; }

    public AddUserSettingsSqlParameters(UsersDbContext? usersDbContext, TuserSetting? userSettings, CancellationToken cancellationToken)
    {
        UsersDbContext = usersDbContext;
        UserSettings = userSettings;
        CancellationToken = cancellationToken;
    }


}

public interface IAddUserSettingsDbService : IServiceHandlerAsync<AddUserSettingsSqlParameters, bool>
{ }

[ScopedService(typeof(IAddUserSettingsDbService))]
public sealed class AddUserSettingsDbService : IAddUserSettingsDbService
{
    async Task<Result<bool>> IServiceHandlerAsync<AddUserSettingsSqlParameters, bool>.HandleAsync(AddUserSettingsSqlParameters @params)
    {
        try
        {
            if (@params.UsersDbContext is null)
                return ResultExceptionFactory.Error<bool>($"{nameof(UsersDbContext)} is null", httpStatusCode: HttpStatusCode.BadRequest);

            if (@params.UserSettings is null)
                return ResultExceptionFactory.Error<bool>($"{nameof(TuserSetting)} is null", httpStatusCode: HttpStatusCode.BadRequest);

            TuserSetting userSetting = @params.UserSettings;

            // Add User Communication
            await @params.UsersDbContext.AddAsync(userSetting, @params.CancellationToken);
            int result = await @params.UsersDbContext.SaveChangesAsync(@params.CancellationToken);

            if (result <= 0)
                return ResultExceptionFactory.Error<bool>("User Settings not added", httpStatusCode: HttpStatusCode.Conflict);


            return Result.Ok(true);
        }
        catch (Exception ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
        {
            return ResultExceptionFactory.Error<bool>("User already exists", httpStatusCode: HttpStatusCode.Conflict);
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error<bool>(ex.Message, httpStatusCode: HttpStatusCode.InternalServerError);
        }
    }
}

#endregion

#region Add User Organization Db Service
public class AddUserOrganizationSqlParameters
{
    public UsersDbContext? UsersDbContext { get; }

    public TusersOrganization? UserOrganization { get; }

    public CancellationToken CancellationToken { get; }

    public AddUserOrganizationSqlParameters(UsersDbContext? usersDbContext, TusersOrganization? userOrganization, CancellationToken cancellationToken)
    {
        UsersDbContext = usersDbContext;
        UserOrganization = userOrganization;
        CancellationToken = cancellationToken;
    }
}

public interface IAddUserOrganizationDbService : IServiceHandlerAsync<AddUserOrganizationSqlParameters, bool>
{ }

[ScopedService(typeof(IAddUserOrganizationDbService))]
public sealed class AddUserOrganizationDbService : IAddUserOrganizationDbService
{
    async Task<Result<bool>> IServiceHandlerAsync<AddUserOrganizationSqlParameters, bool>.HandleAsync(AddUserOrganizationSqlParameters @params)
    {
        try
        {
            if (@params.UsersDbContext is null)
                return ResultExceptionFactory.Error<bool>($"{nameof(UsersDbContext)} is null", httpStatusCode: HttpStatusCode.BadRequest);

            if (@params.UserOrganization is null)
                return ResultExceptionFactory.Error<bool>($"{nameof(TusersOrganization)} is null", httpStatusCode: HttpStatusCode.BadRequest);

            TusersOrganization tusersOrganization = @params.UserOrganization;

            // Add User Communication
            await @params.UsersDbContext.AddAsync(tusersOrganization, @params.CancellationToken);
            int result = await @params.UsersDbContext.SaveChangesAsync(@params.CancellationToken);

            if (result <= 0)
                return ResultExceptionFactory.Error<bool>("User Organization not added", httpStatusCode: HttpStatusCode.Conflict);


            return Result.Ok(true);
        }
        catch (Exception ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
        {
            return ResultExceptionFactory.Error<bool>("User already exists", httpStatusCode: HttpStatusCode.Conflict);
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error<bool>(ex.Message, httpStatusCode: HttpStatusCode.InternalServerError);
        }
    }
}
#endregion 

*/