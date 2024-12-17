
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