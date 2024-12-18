using FluentResults;
using Microsoft.EntityFrameworkCore;
using Models.Shared.Enums;
using sorovi.DependencyInjection.AutoRegister;
using System.Net;
using Users.Infrastructures.Contexts;
using Users.Infrastructures.Entities;
using Utility.Shared.Exceptions;
using Utility.Shared.ServiceHandler;

namespace Users.Infrastructures.Services.GetUserDataByEmailToken;

#region Db Service
public class GetUserDataByEmailTokenDbServiceSqlParameters
{
    public Guid? Token { get; }

    public CancellationToken CancellationToken { get; }

    public GetUserDataByEmailTokenDbServiceSqlParameters(Guid? token, CancellationToken cancellationToken)
    {
        Token = token;
        CancellationToken = cancellationToken;
    }
}

public class GetUserDataByEmailTokenDbServiceResult
{
    public Tuser User { get; }

    public GetUserDataByEmailTokenDbServiceResult(Tuser user)
    {
        User = user;
    }
}

public interface IGetUserDataByEmailTokenDbService : IServiceHandlerAsync<GetUserDataByEmailTokenDbServiceSqlParameters, GetUserDataByEmailTokenDbServiceResult>
{

}

[ScopedService(typeof(IGetUserDataByEmailTokenDbService))]
public class GetUserDataByEmailTokenDbService : IGetUserDataByEmailTokenDbService
{
    private readonly UsersDbContext _usersDbContext;

    public GetUserDataByEmailTokenDbService(UsersDbContext usersDbContext)
    {
        _usersDbContext = usersDbContext;
    }

    async Task<Result<GetUserDataByEmailTokenDbServiceResult>> IServiceHandlerAsync<GetUserDataByEmailTokenDbServiceSqlParameters, GetUserDataByEmailTokenDbServiceResult>
        .HandleAsync(GetUserDataByEmailTokenDbServiceSqlParameters @params)
    {
        try
        {
            if(@params is null)
                return ResultExceptionFactory.Error($"{nameof(GetUserDataByEmailTokenDbServiceSqlParameters)} is null", HttpStatusCode.BadRequest);

            if(@params.Token is null)
                return ResultExceptionFactory.Error($"{nameof(GetUserDataByEmailTokenDbServiceSqlParameters.Token)} is null", HttpStatusCode.BadRequest);

            // Check email is verified
            var isEmailVerified=await _usersDbContext
                .Tusers
                .AsNoTracking()
                .AsQueryable()
                .FirstOrDefaultAsync(x=> x.TuserSetting.IsEmailVerified==Convert.ToBoolean(Convert.ToInt32(VerifiedEnum.Yes)), @params.CancellationToken);

            if(isEmailVerified is not null)
                return ResultExceptionFactory.Error("User is already verified", HttpStatusCode.NotAcceptable);

            // Get User by Email Token
            var user=await _usersDbContext
                .Tusers
                .AsNoTracking()
                .AsQueryable()
                .FirstOrDefaultAsync(x=> x.TuserToken.EmailToken==@params.Token, @params.CancellationToken);

            if(user is null)
                return ResultExceptionFactory.Error("User email token not found, maybe user already verified.", HttpStatusCode.NotFound);

            GetUserDataByEmailTokenDbServiceResult getUserDataByEmailTokenDbServiceResult=
                new GetUserDataByEmailTokenDbServiceResult(user);

            return Result.Ok(getUserDataByEmailTokenDbServiceResult);
        }
        catch(Exception ex)
        {
            return ResultExceptionFactory.Error(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}
#endregion 
