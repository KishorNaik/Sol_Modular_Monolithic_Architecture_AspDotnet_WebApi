
using FluentResults;
using sorovi.DependencyInjection.AutoRegister;
using System.Net;
using Users.Infrastructures.Contexts;
using Users.Infrastructures.Entities;
using Utility.Shared.Exceptions;
using Utility.Shared.ServiceHandler;

namespace Users.Infrastructures.Services.UpdateUseRowVersion;

#region Db Service

public class UpdateUserRowVersionDbServiceParameters
{
    public Tuser User { get; }

    public CancellationToken CancellationToken { get; }

    public UpdateUserRowVersionDbServiceParameters(Tuser tuser,CancellationToken cancellationToken)
    {
        User = tuser;
        CancellationToken = cancellationToken;
    }
}

public interface IUpdateUserRowVersionDbService: IServiceHandlerVoidAsync<UpdateUserRowVersionDbServiceParameters>
{

}

[ScopedService(typeof(IUpdateUserRowVersionDbService))]
public class UpdateUserRowVersionDbService : IUpdateUserRowVersionDbService
{
    private readonly UsersDbContext _usersDbContext;

    public UpdateUserRowVersionDbService(UsersDbContext usersDbContext)
    {
        _usersDbContext = usersDbContext;
    }
    async Task<Result> IServiceHandlerVoidAsync<UpdateUserRowVersionDbServiceParameters>.HandleAsync(UpdateUserRowVersionDbServiceParameters @params)
    {
        try
        {

            if(@params is null)
                return ResultExceptionFactory.Error($"{nameof(@params)} is null", HttpStatusCode.BadRequest);

            if(@params.User is null)
                return ResultExceptionFactory.Error($"{nameof(@params.User)} is null", HttpStatusCode.BadRequest);

            Tuser tuser = @params.User;
            tuser.ModifiedDate= DateTime.UtcNow;

            _usersDbContext.Update<Tuser>(tuser);
            await _usersDbContext.SaveChangesAsync(@params.CancellationToken);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}
#endregion
