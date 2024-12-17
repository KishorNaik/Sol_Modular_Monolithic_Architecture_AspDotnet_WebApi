using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Models.Shared.Enums;
using sorovi.DependencyInjection.AutoRegister;
using System.Net;
using Users.Infrastructures.Contexts;
using Users.Infrastructures.Entities;
using Users.Infrastructures.Services.GetUsersByIdentifier;
using Utility.Shared.Exceptions;
using Utility.Shared.ServiceHandler;

namespace Users.Infrastructures.Services.UpdateRefreshToken;

#region Db Service
public class UpdateRefreshTokenDbServiceParameters
{
   
    public Tuser User { get; }

    public string? RefreshToken { get; }

    public CancellationToken CancellationToken { get; }


    public UpdateRefreshTokenDbServiceParameters(Tuser user, string? refreshToken,CancellationToken cancellationToken)
    {
        User = user;
        RefreshToken = refreshToken;
        CancellationToken = cancellationToken;
    }

}

public interface IUpdateRefreshTokenDbService : IServiceHandlerAsync<UpdateRefreshTokenDbServiceParameters, Unit>
{

}

[ScopedService(typeof(IUpdateRefreshTokenDbService))]
public class UpdateRefreshTokenDbService : IUpdateRefreshTokenDbService
{
    private readonly UsersDbContext _usersDbContext;

    public UpdateRefreshTokenDbService(UsersDbContext usersDbContext)
    {
        _usersDbContext = usersDbContext;
    }

    async Task<Result<Unit>> IServiceHandlerAsync<UpdateRefreshTokenDbServiceParameters, Unit>.HandleAsync(UpdateRefreshTokenDbServiceParameters @params)
    {
        try
        {
            if(@params is null)
                return ResultExceptionFactory.Error<Unit>($"{nameof(UpdateRefreshTokenDbServiceParameters)} is null", HttpStatusCode.BadRequest);

            if(@params.User is null)
                return ResultExceptionFactory.Error<Unit>($"{nameof(@params.User)} is null", HttpStatusCode.BadRequest);

            if(@params.RefreshToken is null)
                return ResultExceptionFactory.Error<Unit>($"{nameof(@params.RefreshToken)} is null", HttpStatusCode.BadRequest);

            
            // Get User Identifier
            Guid? identifier = @params.User.Identifier;
            if(identifier is null)
                return ResultExceptionFactory.Error<Unit>($"{nameof(identifier)} is not found", HttpStatusCode.NotFound);

            // get Updated Refresh Token from GetUserByIdentiferResult
            string? refreshToken = @params.RefreshToken;
            if(refreshToken is null)
                return ResultExceptionFactory.Error<Unit>($"{nameof(refreshToken)} is not found", HttpStatusCode.NotFound);

            // Update Refresh Token
            TuserToken tuserToken = @params.User.TuserToken;

            if(tuserToken is null)
                return ResultExceptionFactory.Error<Unit>($"{nameof(tuserToken)} is not found", HttpStatusCode.NotFound);

            tuserToken.RefreshToken = refreshToken;
            tuserToken.RefreshTokenExpirayTime = DateTime.UtcNow.AddDays(7);

            _usersDbContext.Update<TuserToken>(tuserToken);
            await _usersDbContext.SaveChangesAsync(@params.CancellationToken);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error<Unit>(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}
#endregion 
